from datetime import datetime

from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy.orm import Session, joinedload

import models
import schemas
from database import get_db


router = APIRouter(
    prefix="/battle",
    tags=["battle"],
)


def _normalize_required_exp(user_status):
    if user_status.required_exp is None or user_status.required_exp <= 0:
        user_status.required_exp = 1000


def _apply_player_exp(user_status, reward_exp: int):
    reward_exp = max(0, reward_exp)

    _normalize_required_exp(user_status)

    if user_status.player_exp is None:
        user_status.player_exp = 0

    if user_status.player_level is None or user_status.player_level <= 0:
        user_status.player_level = 1

    user_status.player_exp += reward_exp

    while user_status.player_exp >= user_status.required_exp:
        user_status.player_exp -= user_status.required_exp
        user_status.player_level += 1

        # main.py의 calculate_required_exp와 같은 계열: 레벨당 약 16% 증가
        user_status.required_exp = int(1000 * (1.16 ** (user_status.player_level - 1)))


def _battle_response(
    *,
    success: bool,
    user_status,
    user_currency,
    message: str,
    stage_id: int | None = None,
    is_clear: bool | None = None,
    reward_gold: int = 0,
    reward_exp: int = 0,
    kill_count_add: int = 0,
):
    """
    Unity 쪽 호환을 위해 player_exp/player_level과 exp/level 둘 다 내려줌.
    경험치바 코드가 exp를 보든 player_exp를 보든 둘 다 받을 수 있게 함.
    """

    response = {
        "success": success,
        "user_id": user_status.user_id,
        "message": message,

        "current_stage": user_status.current_stage,
        "max_cleared_stage": user_status.max_cleared_stage,
        "total_boss_kill_count": user_status.total_boss_kill_count,

        "player_level": user_status.player_level,
        "player_exp": user_status.player_exp,
        "required_exp": user_status.required_exp,

        # Unity 구버전 응답 클래스 호환용 alias
        "level": user_status.player_level,
        "exp": user_status.player_exp,

        "gold": user_currency.gold if user_currency else 0,
        "gem": user_currency.gem if user_currency else 0,
    }

    if stage_id is not None:
        response["stage_id"] = stage_id

    if is_clear is not None:
        response["is_clear"] = is_clear

    response["reward_gold"] = reward_gold
    response["reward_exp"] = reward_exp
    response["kill_count_add"] = kill_count_add

    return response


@router.get(
    "/status/{user_id}",
    response_model=schemas.UserStatusResponse,
)
def get_user_battle_status(
    user_id: int,
    db: Session = Depends(get_db),
):
    user_status = (
        db.query(models.UserStatus)
        .options(joinedload(models.UserStatus.current_character))
        .filter(models.UserStatus.user_id == user_id)
        .first()
    )

    if user_status is None:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="유저 상태 정보를 찾을 수 없습니다.",
        )

    return user_status


@router.post("/challenge-stage/{user_id}")
def challenge_stage(
    user_id: int,
    db: Session = Depends(get_db),
):
    user_status = (
        db.query(models.UserStatus)
        .filter(models.UserStatus.user_id == user_id)
        .first()
    )

    if user_status is None:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="유저 상태 정보를 찾을 수 없습니다.",
        )

    user_currency = (
        db.query(models.UserCurrency)
        .filter(models.UserCurrency.user_id == user_id)
        .first()
    )

    if user_currency is None:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="유저 재화 정보를 찾을 수 없습니다.",
        )

    if user_status.current_stage is None or user_status.current_stage <= 0:
        user_status.current_stage = 1

    if user_status.max_cleared_stage is None or user_status.max_cleared_stage < 0:
        user_status.max_cleared_stage = 0

    _normalize_required_exp(user_status)

    # 현재 스테이지가 아직 클리어되지 않은 상태면 다음 스테이지 도전 불가
    # 예: current_stage=2, max_cleared_stage=1이면 2스테이지 도전 중/미클리어 상태
    if user_status.current_stage > user_status.max_cleared_stage:
        return _battle_response(
            success=False,
            user_status=user_status,
            user_currency=user_currency,
            message="현재 스테이지를 먼저 클리어해야 다음 스테이지에 도전할 수 있습니다.",
        )

    # 현재 스테이지를 이미 깬 상태면 도전 버튼으로만 다음 스테이지 이동
    user_status.current_stage += 1
    user_status.updated_at = datetime.now()
    user_currency.updated_at = datetime.now()

    db.commit()
    db.refresh(user_status)
    db.refresh(user_currency)

    return _battle_response(
        success=True,
        user_status=user_status,
        user_currency=user_currency,
        message="다음 스테이지 도전 시작",
    )


@router.post("/reward")
def save_battle_reward(
    request: schemas.BattleRewardRequest,
    db: Session = Depends(get_db),
):
    user_status = (
        db.query(models.UserStatus)
        .filter(models.UserStatus.user_id == request.user_id)
        .first()
    )

    if user_status is None:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="유저 상태 정보를 찾을 수 없습니다.",
        )

    user_currency = (
        db.query(models.UserCurrency)
        .filter(models.UserCurrency.user_id == request.user_id)
        .first()
    )

    if user_currency is None:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="유저 재화 정보를 찾을 수 없습니다.",
        )

    if user_status.current_stage is None or user_status.current_stage <= 0:
        user_status.current_stage = 1

    if user_status.max_cleared_stage is None or user_status.max_cleared_stage < 0:
        user_status.max_cleared_stage = 0

    if user_status.total_boss_kill_count is None:
        user_status.total_boss_kill_count = 0

    _normalize_required_exp(user_status)

    # 패배 처리
    # 못 깨면 이전 클리어 스테이지로 복귀
    # 단, max_cleared_stage가 0이면 최소 1스테이지로 유지
    if request.is_clear is False:
        user_status.current_stage = max(1, user_status.max_cleared_stage)

        user_status.updated_at = datetime.now()
        user_currency.updated_at = datetime.now()

        db.commit()
        db.refresh(user_status)
        db.refresh(user_currency)

        return _battle_response(
            success=True,
            user_status=user_status,
            user_currency=user_currency,
            stage_id=request.stage_id,
            is_clear=False,
            message="패배 처리 완료. 이전 스테이지로 복귀했습니다.",
            reward_gold=0,
            reward_exp=0,
            kill_count_add=0,
        )

    # 승리 처리
    # 여기서는 current_stage를 절대 +1 하지 않음
    # 도전 버튼을 눌렀을 때만 다음 스테이지로 이동해야 함

    user_currency.gold += request.reward_gold

    _apply_player_exp(
        user_status=user_status,
        reward_exp=request.reward_exp,
    )

    user_status.total_boss_kill_count += request.kill_count_add

    if request.stage_id > user_status.max_cleared_stage:
        user_status.max_cleared_stage = request.stage_id

    # 승리한 스테이지를 현재 스테이지로 유지
    # 이래야 "깬 스테이지 반복" 가능
    user_status.current_stage = request.stage_id

    user_status.updated_at = datetime.now()
    user_currency.updated_at = datetime.now()

    db.commit()
    db.refresh(user_status)
    db.refresh(user_currency)

    return _battle_response(
        success=True,
        user_status=user_status,
        user_currency=user_currency,
        stage_id=request.stage_id,
        is_clear=True,
        message="전투 보상 저장 완료",
        reward_gold=request.reward_gold,
        reward_exp=request.reward_exp,
        kill_count_add=request.kill_count_add,
    )