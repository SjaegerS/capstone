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

    currency = (
        db.query(models.UserCurrency)
        .filter(models.UserCurrency.user_id == request.user_id)
        .first()
    )

    if currency is None:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="재화 정보를 찾을 수 없습니다.",
        )

    if request.stage_id <= 0:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail="stage_id가 올바르지 않습니다.",
        )

    if request.reward_gold < 0:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail="reward_gold는 음수일 수 없습니다.",
        )

    if request.reward_exp < 0:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail="reward_exp는 음수일 수 없습니다.",
        )

    if request.kill_count_add < 0:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail="kill_count_add는 음수일 수 없습니다.",
        )

    cleared_stage = request.stage_id
    next_stage = cleared_stage + 1

    user_status.current_stage = max(
        user_status.current_stage,
        next_stage,
    )

    user_status.player_exp += request.reward_exp
    user_status.total_boss_kill_count += request.kill_count_add
    user_status.updated_at = datetime.now()

    currency.gold += request.reward_gold
    currency.updated_at = datetime.now()

    db.commit()
    db.refresh(user_status)
    db.refresh(currency)

    return {
        "success": True,
        "message": "전투 보상이 저장되었습니다.",
        "user_id": request.user_id,
        "cleared_stage": cleared_stage,
        "current_stage": user_status.current_stage,
        "gold": currency.gold,
        "gem": currency.gem,
        "exp": user_status.player_exp,
        "level": user_status.player_level,
        "required_exp": user_status.required_exp,
        "total_boss_kill_count": user_status.total_boss_kill_count,
        "max_hp": user_status.max_hp,
        "attack_power": user_status.attack_power,
        "defense_power": user_status.defense_power,
    }