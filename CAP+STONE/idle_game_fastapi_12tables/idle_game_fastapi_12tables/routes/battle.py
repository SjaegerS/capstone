from datetime import datetime

from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy import text
from sqlalchemy.orm import Session

import schemas
from database import get_db


router = APIRouter(
    prefix="/battle",
    tags=["battle"],
)


LEVEL_UP_GEM_REWARD = 500


def calculate_required_exp(level: int) -> int:
    safe_level = max(1, level)
    return int(1000 * (1.16 ** (safe_level - 1)))


def get_user_status_row(db: Session, user_id: int):
    row = db.execute(
        text(
            """
            SELECT
                us.user_id,
                us.current_character_id,

                us.player_level,
                us.player_exp,
                us.required_exp,

                us.hp_upgrade_lvl,
                us.attack_upgrade_lvl,
                us.defense_upgrade_lvl,

                us.max_hp,
                us.attack_power,
                us.defense_power,

                us.current_stage,
                us.max_cleared_stage,
                us.total_boss_kill_count,
                us.updated_at,

                ci.character_id,
                ci.character_key,
                ci.character_name,

                COALESCE(uc.gold, 0) AS gold,
                COALESCE(uc.gem, 0) AS gem
            FROM user_status us
            LEFT JOIN character_info ci
                ON us.current_character_id = ci.character_id
            LEFT JOIN user_currency uc
                ON us.user_id = uc.user_id
            WHERE us.user_id = :user_id
            """
        ),
        {"user_id": user_id},
    ).mappings().first()

    if row is None:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="유저 상태 정보를 찾을 수 없습니다.",
        )

    return row


@router.get("/ping")
def battle_ping():
    return {
        "success": True,
        "message": "battle router connected",
    }


@router.get("/status/{user_id}")
def get_user_battle_status(
    user_id: int,
    db: Session = Depends(get_db),
):
    row = get_user_status_row(db, user_id)
    result = dict(row)

    character_id = result.pop("character_id", None)
    character_key = result.pop("character_key", None)
    character_name = result.pop("character_name", None)

    result["current_character"] = None

    if character_id is not None:
        result["current_character"] = {
            "character_id": int(character_id),
            "character_key": character_key,
            "character_name": character_name,
        }

    return result


@router.post("/challenge-stage/{user_id}")
def challenge_stage(
    user_id: int,
    db: Session = Depends(get_db),
):
    """
    도전 버튼을 눌렀을 때 호출.

    current_stage를 1 증가시켜 다음 스테이지에 도전하게 한다.
    실패하면 /battle/reward에서 max_cleared_stage 기준으로 복구한다.
    """
    try:
        row = db.execute(
            text(
                """
                SELECT
                    user_id,
                    current_stage,
                    max_cleared_stage
                FROM user_status
                WHERE user_id = :user_id
                """
            ),
            {"user_id": user_id},
        ).mappings().first()

        if row is None:
            raise HTTPException(
                status_code=status.HTTP_404_NOT_FOUND,
                detail="유저 상태 정보를 찾을 수 없습니다.",
            )

        current_stage = int(row["current_stage"] or 1)
        max_cleared_stage = int(row["max_cleared_stage"] or 0)

        challenge_stage_value = current_stage + 1

        db.execute(
            text(
                """
                UPDATE user_status
                SET
                    current_stage = :current_stage,
                    updated_at = :updated_at
                WHERE user_id = :user_id
                """
            ),
            {
                "user_id": user_id,
                "current_stage": challenge_stage_value,
                "updated_at": datetime.now(),
            },
        )

        db.commit()

        return {
            "success": True,
            "user_id": user_id,
            "previous_stage": current_stage,
            "current_stage": challenge_stage_value,
            "max_cleared_stage": max_cleared_stage,
            "can_challenge": True,
        }

    except HTTPException:
        db.rollback()
        raise

    except Exception as e:
        db.rollback()
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"stage challenge 처리 중 서버 오류: {str(e)}",
        )


@router.post("/reward")
def save_battle_reward(
    request: schemas.BattleRewardRequest,
    db: Session = Depends(get_db),
):
    """
    전투 결과 보상 저장.

    성공:
    - 골드 지급
    - 경험치 지급
    - 레벨업 처리
    - 레벨업 1회당 젬 500 지급
    - max_cleared_stage 갱신
    - current_stage는 방금 깬 스테이지로 유지

    실패:
    - 보상 없음
    - current_stage를 max_cleared_stage로 복구
    - max_cleared_stage가 0이면 current_stage = 1
    """
    try:
        status_row = db.execute(
            text(
                """
                SELECT
                    user_id,
                    player_level,
                    player_exp,
                    required_exp,
                    current_stage,
                    max_cleared_stage,
                    total_boss_kill_count
                FROM user_status
                WHERE user_id = :user_id
                """
            ),
            {"user_id": request.user_id},
        ).mappings().first()

        if status_row is None:
            raise HTTPException(
                status_code=status.HTTP_404_NOT_FOUND,
                detail="유저 상태 정보를 찾을 수 없습니다.",
            )

        currency_row = db.execute(
            text(
                """
                SELECT
                    user_id,
                    gold,
                    gem
                FROM user_currency
                WHERE user_id = :user_id
                """
            ),
            {"user_id": request.user_id},
        ).mappings().first()

        if currency_row is None:
            raise HTTPException(
                status_code=status.HTTP_404_NOT_FOUND,
                detail="유저 재화 정보를 찾을 수 없습니다.",
            )

        user_id = int(request.user_id)

        player_level = int(status_row["player_level"] or 1)
        player_exp = int(status_row["player_exp"] or 0)
        required_exp = int(
            status_row["required_exp"] or calculate_required_exp(player_level)
        )

        current_stage = int(status_row["current_stage"] or 1)
        max_cleared_stage = int(status_row["max_cleared_stage"] or 0)
        total_boss_kill_count = int(status_row["total_boss_kill_count"] or 0)

        gold = int(currency_row["gold"] or 0)
        gem = int(currency_row["gem"] or 0)

        request_stage = int(request.stage_id or current_stage)

        reward_gold = 0
        reward_exp = 0
        reward_gem = 0
        level_up_count = 0

        if request.is_clear:
            reward_gold = int(request.reward_gold or 0)
            reward_exp = int(request.reward_exp or 0)

            gold += reward_gold
            player_exp += reward_exp
            total_boss_kill_count += int(request.kill_count_add or 0)

            while player_exp >= required_exp:
                player_exp -= required_exp
                player_level += 1
                level_up_count += 1

                reward_gem += LEVEL_UP_GEM_REWARD
                required_exp = calculate_required_exp(player_level)

            gem += reward_gem

            if request_stage > max_cleared_stage:
                max_cleared_stage = request_stage

            current_stage = request_stage

        else:
            current_stage = max(1, max_cleared_stage)

            reward_gold = 0
            reward_exp = 0
            reward_gem = 0

        now = datetime.now()

        db.execute(
            text(
                """
                UPDATE user_status
                SET
                    player_level = :player_level,
                    player_exp = :player_exp,
                    required_exp = :required_exp,
                    current_stage = :current_stage,
                    max_cleared_stage = :max_cleared_stage,
                    total_boss_kill_count = :total_boss_kill_count,
                    updated_at = :updated_at
                WHERE user_id = :user_id
                """
            ),
            {
                "user_id": user_id,
                "player_level": player_level,
                "player_exp": player_exp,
                "required_exp": required_exp,
                "current_stage": current_stage,
                "max_cleared_stage": max_cleared_stage,
                "total_boss_kill_count": total_boss_kill_count,
                "updated_at": now,
            },
        )

        db.execute(
            text(
                """
                UPDATE user_currency
                SET
                    gold = :gold,
                    gem = :gem,
                    updated_at = :updated_at
                WHERE user_id = :user_id
                """
            ),
            {
                "user_id": user_id,
                "gold": gold,
                "gem": gem,
                "updated_at": now,
            },
        )

        db.commit()

        return {
            "success": True,
            "user_id": user_id,
            "is_clear": bool(request.is_clear),

            "player_level": player_level,
            "player_exp": player_exp,
            "required_exp": required_exp,

            "level": player_level,
            "exp": player_exp,

            "gold": gold,
            "gem": gem,

            "reward_gold": reward_gold,
            "reward_exp": reward_exp,
            "reward_gem": reward_gem,

            "level_up": level_up_count > 0,
            "level_up_count": level_up_count,

            "current_stage": current_stage,
            "max_cleared_stage": max_cleared_stage,
            "total_boss_kill_count": total_boss_kill_count,
        }

    except HTTPException:
        db.rollback()
        raise

    except Exception as e:
        db.rollback()
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"battle reward 처리 중 서버 오류: {str(e)}",
        )