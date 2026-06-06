from datetime import datetime

from fastapi import APIRouter, Depends, HTTPException, status
from pydantic import BaseModel, Field
from sqlalchemy.orm import Session, joinedload

import models
import schemas
from database import get_db


router = APIRouter(
    prefix="/battle",
    tags=["battle"],
)


class StageChallengeRequest(BaseModel):
    user_id: int
    stage_id: int = Field(ge=1)


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

    return {
        "success": True,
        "user_id": user_id,
        "current_stage": user_status.current_stage,
        "max_cleared_stage": user_status.max_cleared_stage,
        "message": "스테이지 도전 가능",
    }


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

    # 패배 처리
    # is_clear가 False면 보상, 경험치, 스테이지 증가 전부 하지 않음
    if request.is_clear is False:
        user_status.updated_at = datetime.now()
        user_currency.updated_at = datetime.now()

        db.commit()
        db.refresh(user_status)
        db.refresh(user_currency)

        return {
            "user_id": request.user_id,
            "stage_id": request.stage_id,
            "is_clear": False,
            "message": "패배 처리 완료. 스테이지와 보상은 변경되지 않았습니다.",
            "current_stage": user_status.current_stage,
            "max_cleared_stage": user_status.max_cleared_stage,
            "total_boss_kill_count": user_status.total_boss_kill_count,
            "player_level": user_status.player_level,
            "player_exp": user_status.player_exp,
            "required_exp": user_status.required_exp,
            "gold": user_currency.gold,
            "gem": user_currency.gem,
        }

    # 승리 처리
    user_currency.gold += request.reward_gold
    user_status.player_exp += request.reward_exp
    user_status.total_boss_kill_count += request.kill_count_add

    # 레벨업 처리
    while user_status.player_exp >= user_status.required_exp:
        user_status.player_exp -= user_status.required_exp
        user_status.player_level += 1
        user_status.required_exp = int(user_status.required_exp * 1.2)

    # 클리어한 스테이지가 현재 스테이지 이상이면 다음 스테이지 해금
    if request.stage_id >= user_status.current_stage:
        user_status.current_stage = request.stage_id + 1

    # 최고 클리어 스테이지 갱신
    if request.stage_id > user_status.max_cleared_stage:
        user_status.max_cleared_stage = request.stage_id

    user_status.updated_at = datetime.now()
    user_currency.updated_at = datetime.now()

    db.commit()
    db.refresh(user_status)
    db.refresh(user_currency)

    return {
        "user_id": request.user_id,
        "stage_id": request.stage_id,
        "is_clear": True,
        "message": "전투 보상 저장 완료",
        "reward_gold": request.reward_gold,
        "reward_exp": request.reward_exp,
        "kill_count_add": request.kill_count_add,
        "current_stage": user_status.current_stage,
        "max_cleared_stage": user_status.max_cleared_stage,
        "total_boss_kill_count": user_status.total_boss_kill_count,
        "player_level": user_status.player_level,
        "player_exp": user_status.player_exp,
        "required_exp": user_status.required_exp,
        "gold": user_currency.gold,
        "gem": user_currency.gem,
    }