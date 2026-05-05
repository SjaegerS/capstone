from fastapi import FastAPI, Depends, HTTPException, status
from sqlalchemy.orm import Session, joinedload
from sqlalchemy.exc import IntegrityError
from typing import List
from datetime import date, datetime

import models
import schemas
from database import get_db

app = FastAPI(
    title="Idle Game API",
    description="12개 테이블 기준 Unity 방치형 게임 + 사용시간 기록 + 퀘스트 + AI 피드백 API",
    version="1.0.0",
)


def get_or_404(db: Session, model, condition, detail: str):
    obj = db.query(model).filter(condition).first()
    if not obj:
        raise HTTPException(status_code=404, detail=detail)
    return obj


def apply_update(obj, update_schema):
    update_data = update_schema.model_dump(exclude_unset=True)
    for field, value in update_data.items():
        setattr(obj, field, value)
    return obj


@app.get("/health")
def health_check():
    return {"status": "ok"}


# --------------------
# Character Types
# --------------------

@app.get("/character-types/", response_model=List[schemas.CharacterTypeResponse])
def get_character_types(db: Session = Depends(get_db)):
    return db.query(models.CharacterType).order_by(models.CharacterType.character_type_id).all()


# --------------------
# Users
# --------------------

@app.post("/users/", response_model=schemas.UserResponse, status_code=status.HTTP_201_CREATED)
def create_user(user_data: schemas.UserCreate, db: Session = Depends(get_db)):
    character_type = db.query(models.CharacterType).filter(
        models.CharacterType.character_type_id == user_data.default_character_type_id
    ).first()

    if not character_type:
        raise HTTPException(status_code=404, detail="기본 캐릭터 유형을 찾을 수 없습니다.")

    exists_email = db.query(models.User).filter(models.User.email == user_data.email).first()
    if exists_email:
        raise HTTPException(status_code=409, detail="이미 사용 중인 이메일입니다.")

    user = models.User(
        email=str(user_data.email),
        password_hash=user_data.password_hash,
        nickname=user_data.nickname,
    )

    try:
        db.add(user)
        db.flush()

        # 1. 기본 캐릭터 상태 생성
        character_status = models.CharacterStatus(
            user_id=user.user_id,
            character_type_id=user_data.default_character_type_id,
        )
        db.add(character_status)

        # 2. 기본 재화 생성
        currency = models.UserCurrency(
            user_id=user.user_id,
            gold=0,
            gem=0,
        )
        db.add(currency)

        # 3. 캐릭터 유형별 컨디션 4개 생성
        character_types = db.query(models.CharacterType).all()
        today = date.today()

        for ctype in character_types:
            condition = models.CharacterCondition(
                user_id=user.user_id,
                character_type_id=ctype.character_type_id,
                condition_score=3,
                condition_grade="NORMAL",
                last_updated_date=today,
            )
            db.add(condition)

        db.commit()
        db.refresh(user)
        return user

    except IntegrityError:
        db.rollback()
        raise HTTPException(status_code=400, detail="사용자 생성 중 무결성 오류가 발생했습니다.")


@app.get("/users/", response_model=List[schemas.UserResponse])
def get_users(skip: int = 0, limit: int = 100, db: Session = Depends(get_db)):
    return db.query(models.User).offset(skip).limit(limit).all()


@app.get("/users/{user_id}", response_model=schemas.UserDetailResponse)
def get_user_detail(user_id: int, db: Session = Depends(get_db)):
    user = (
        db.query(models.User)
        .options(
            joinedload(models.User.character_status).joinedload(models.CharacterStatus.character_type),
            joinedload(models.User.currency),
            joinedload(models.User.conditions).joinedload(models.CharacterCondition.character_type),
            joinedload(models.User.user_items).joinedload(models.UserItem.item),
            joinedload(models.User.user_quests).joinedload(models.UserQuest.quest),
            joinedload(models.User.usage_logs),
            joinedload(models.User.ai_feedbacks),
            joinedload(models.User.offline_reward_boxes),
        )
        .filter(models.User.user_id == user_id)
        .first()
    )

    if not user:
        raise HTTPException(status_code=404, detail="사용자를 찾을 수 없습니다.")

    return user


@app.patch("/users/{user_id}", response_model=schemas.UserResponse)
def update_user(user_id: int, user_update: schemas.UserUpdate, db: Session = Depends(get_db)):
    user = get_or_404(db, models.User, models.User.user_id == user_id, "사용자를 찾을 수 없습니다.")
    apply_update(user, user_update)
    db.commit()
    db.refresh(user)
    return user


@app.delete("/users/{user_id}", status_code=status.HTTP_204_NO_CONTENT)
def delete_user(user_id: int, db: Session = Depends(get_db)):
    user = get_or_404(db, models.User, models.User.user_id == user_id, "사용자를 찾을 수 없습니다.")
    db.delete(user)
    db.commit()
    return None


# --------------------
# Character Status
# --------------------

@app.get("/users/{user_id}/character-status/", response_model=schemas.CharacterStatusResponse)
def get_character_status(user_id: int, db: Session = Depends(get_db)):
    get_or_404(db, models.User, models.User.user_id == user_id, "사용자를 찾을 수 없습니다.")

    character_status = (
        db.query(models.CharacterStatus)
        .options(joinedload(models.CharacterStatus.character_type))
        .filter(models.CharacterStatus.user_id == user_id)
        .first()
    )

    if not character_status:
        raise HTTPException(status_code=404, detail="캐릭터 상태를 찾을 수 없습니다.")

    return character_status


@app.patch("/users/{user_id}/character-status/", response_model=schemas.CharacterStatusResponse)
def update_character_status(
    user_id: int,
    status_update: schemas.CharacterStatusUpdate,
    db: Session = Depends(get_db),
):
    character_status = get_or_404(
        db,
        models.CharacterStatus,
        models.CharacterStatus.user_id == user_id,
        "캐릭터 상태를 찾을 수 없습니다.",
    )

    if status_update.character_type_id is not None:
        get_or_404(
            db,
            models.CharacterType,
            models.CharacterType.character_type_id == status_update.character_type_id,
            "캐릭터 유형을 찾을 수 없습니다.",
        )

    apply_update(character_status, status_update)
    db.commit()
    db.refresh(character_status)
    return character_status


# --------------------
# Character Conditions
# --------------------

@app.get("/users/{user_id}/conditions/", response_model=List[schemas.CharacterConditionResponse])
def get_user_conditions(user_id: int, db: Session = Depends(get_db)):
    get_or_404(db, models.User, models.User.user_id == user_id, "사용자를 찾을 수 없습니다.")

    return (
        db.query(models.CharacterCondition)
        .options(joinedload(models.CharacterCondition.character_type))
        .filter(models.CharacterCondition.user_id == user_id)
        .order_by(models.CharacterCondition.character_type_id)
        .all()
    )


@app.patch("/conditions/{condition_id}", response_model=schemas.CharacterConditionResponse)
def update_condition(
    condition_id: int,
    condition_update: schemas.CharacterConditionUpdate,
    db: Session = Depends(get_db),
):
    condition = get_or_404(
        db,
        models.CharacterCondition,
        models.CharacterCondition.condition_id == condition_id,
        "컨디션 정보를 찾을 수 없습니다.",
    )

    apply_update(condition, condition_update)

    if condition_update.last_updated_date is None:
        condition.last_updated_date = date.today()

    db.commit()
    db.refresh(condition)
    return condition


# --------------------
# Currency
# --------------------

@app.get("/users/{user_id}/currency/", response_model=schemas.UserCurrencyResponse)
def get_user_currency(user_id: int, db: Session = Depends(get_db)):
    get_or_404(db, models.User, models.User.user_id == user_id, "사용자를 찾을 수 없습니다.")

    currency = db.query(models.UserCurrency).filter(models.UserCurrency.user_id == user_id).first()
    if not currency:
        raise HTTPException(status_code=404, detail="재화 정보를 찾을 수 없습니다.")
    return currency


@app.patch("/users/{user_id}/currency/", response_model=schemas.UserCurrencyResponse)
def update_user_currency(
    user_id: int,
    currency_update: schemas.UserCurrencyUpdate,
    db: Session = Depends(get_db),
):
    currency = get_or_404(
        db,
        models.UserCurrency,
        models.UserCurrency.user_id == user_id,
        "재화 정보를 찾을 수 없습니다.",
    )

    apply_update(currency, currency_update)
    db.commit()
    db.refresh(currency)
    return currency


# --------------------
# Items
# --------------------

@app.post("/items/", response_model=schemas.ItemResponse, status_code=status.HTTP_201_CREATED)
def create_item(item_data: schemas.ItemCreate, db: Session = Depends(get_db)):
    item = models.Item(**item_data.model_dump())
    db.add(item)
    db.commit()
    db.refresh(item)
    return item


@app.get("/items/", response_model=List[schemas.ItemResponse])
def get_items(skip: int = 0, limit: int = 100, db: Session = Depends(get_db)):
    return db.query(models.Item).offset(skip).limit(limit).all()


@app.patch("/items/{item_id}", response_model=schemas.ItemResponse)
def update_item(item_id: int, item_update: schemas.ItemUpdate, db: Session = Depends(get_db)):
    item = get_or_404(db, models.Item, models.Item.item_id == item_id, "아이템을 찾을 수 없습니다.")
    apply_update(item, item_update)
    db.commit()
    db.refresh(item)
    return item


# --------------------
# User Items
# --------------------

@app.post("/user-items/", response_model=schemas.UserItemResponse, status_code=status.HTTP_201_CREATED)
def create_user_item(user_item_data: schemas.UserItemCreate, db: Session = Depends(get_db)):
    get_or_404(db, models.User, models.User.user_id == user_item_data.user_id, "사용자를 찾을 수 없습니다.")
    get_or_404(db, models.Item, models.Item.item_id == user_item_data.item_id, "아이템을 찾을 수 없습니다.")

    user_item = models.UserItem(**user_item_data.model_dump())
    db.add(user_item)
    db.commit()
    db.refresh(user_item)
    return user_item


@app.get("/users/{user_id}/items/", response_model=List[schemas.UserItemResponse])
def get_user_items(user_id: int, db: Session = Depends(get_db)):
    get_or_404(db, models.User, models.User.user_id == user_id, "사용자를 찾을 수 없습니다.")

    return (
        db.query(models.UserItem)
        .options(joinedload(models.UserItem.item))
        .filter(models.UserItem.user_id == user_id)
        .order_by(models.UserItem.obtained_at.desc())
        .all()
    )


@app.patch("/user-items/{user_item_id}", response_model=schemas.UserItemResponse)
def update_user_item(
    user_item_id: int,
    user_item_update: schemas.UserItemUpdate,
    db: Session = Depends(get_db),
):
    user_item = get_or_404(
        db,
        models.UserItem,
        models.UserItem.user_item_id == user_item_id,
        "보유 아이템을 찾을 수 없습니다.",
    )

    apply_update(user_item, user_item_update)
    db.commit()
    db.refresh(user_item)
    return user_item


# --------------------
# Quests
# --------------------

@app.post("/quests/", response_model=schemas.QuestResponse, status_code=status.HTTP_201_CREATED)
def create_quest(quest_data: schemas.QuestCreate, db: Session = Depends(get_db)):
    quest = models.Quest(**quest_data.model_dump())
    db.add(quest)
    db.commit()
    db.refresh(quest)
    return quest


@app.get("/quests/", response_model=List[schemas.QuestResponse])
def get_quests(skip: int = 0, limit: int = 100, active_only: bool = False, db: Session = Depends(get_db)):
    query = db.query(models.Quest)
    if active_only:
        query = query.filter(models.Quest.is_active == True)
    return query.offset(skip).limit(limit).all()


@app.patch("/quests/{quest_id}", response_model=schemas.QuestResponse)
def update_quest(quest_id: int, quest_update: schemas.QuestUpdate, db: Session = Depends(get_db)):
    quest = get_or_404(db, models.Quest, models.Quest.quest_id == quest_id, "퀘스트를 찾을 수 없습니다.")
    apply_update(quest, quest_update)
    db.commit()
    db.refresh(quest)
    return quest


# --------------------
# User Quests
# --------------------

@app.post("/user-quests/", response_model=schemas.UserQuestResponse, status_code=status.HTTP_201_CREATED)
def create_user_quest(user_quest_data: schemas.UserQuestCreate, db: Session = Depends(get_db)):
    get_or_404(db, models.User, models.User.user_id == user_quest_data.user_id, "사용자를 찾을 수 없습니다.")
    get_or_404(db, models.Quest, models.Quest.quest_id == user_quest_data.quest_id, "퀘스트를 찾을 수 없습니다.")

    data = user_quest_data.model_dump()
    if data["assigned_date"] is None:
        data["assigned_date"] = date.today()

    user_quest = models.UserQuest(**data)
    db.add(user_quest)
    db.commit()
    db.refresh(user_quest)
    return user_quest


@app.post("/users/{user_id}/assign-basic-quests/")
def assign_basic_quests(user_id: int, count: int = 3, db: Session = Depends(get_db)):
    get_or_404(db, models.User, models.User.user_id == user_id, "사용자를 찾을 수 없습니다.")

    today = date.today()
    quests = (
        db.query(models.Quest)
        .filter(models.Quest.is_active == True)
        .limit(count)
        .all()
    )

    if not quests:
        raise HTTPException(status_code=404, detail="등록된 활성 퀘스트가 없습니다.")

    assigned_count = 0

    for quest in quests:
        exists = (
            db.query(models.UserQuest)
            .filter(
                models.UserQuest.user_id == user_id,
                models.UserQuest.quest_id == quest.quest_id,
                models.UserQuest.assigned_date == today,
            )
            .first()
        )

        if exists:
            continue

        user_quest = models.UserQuest(
            user_id=user_id,
            quest_id=quest.quest_id,
            progress_value=0,
            is_accepted=True,
            is_completed=False,
            is_reward_claimed=False,
            assigned_date=today,
        )

        db.add(user_quest)
        assigned_count += 1

    db.commit()

    return {
        "message": "기본 퀘스트 할당 완료",
        "assigned_count": assigned_count,
    }


@app.get("/users/{user_id}/quests/", response_model=List[schemas.UserQuestResponse])
def get_user_quests(user_id: int, db: Session = Depends(get_db)):
    get_or_404(db, models.User, models.User.user_id == user_id, "사용자를 찾을 수 없습니다.")

    return (
        db.query(models.UserQuest)
        .options(joinedload(models.UserQuest.quest))
        .filter(models.UserQuest.user_id == user_id)
        .order_by(models.UserQuest.assigned_date.desc())
        .all()
    )


@app.patch("/user-quests/{user_quest_id}", response_model=schemas.UserQuestResponse)
def update_user_quest(
    user_quest_id: int,
    user_quest_update: schemas.UserQuestUpdate,
    db: Session = Depends(get_db),
):
    user_quest = get_or_404(
        db,
        models.UserQuest,
        models.UserQuest.user_quest_id == user_quest_id,
        "사용자 퀘스트를 찾을 수 없습니다.",
    )

    apply_update(user_quest, user_quest_update)

    if user_quest.is_completed and user_quest.completed_at is None:
        user_quest.completed_at = datetime.now()

    db.commit()
    db.refresh(user_quest)
    return user_quest


@app.patch("/user-quests/{user_quest_id}/complete/", response_model=schemas.UserQuestResponse)
def complete_user_quest(user_quest_id: int, db: Session = Depends(get_db)):
    user_quest = (
        db.query(models.UserQuest)
        .options(joinedload(models.UserQuest.quest))
        .filter(models.UserQuest.user_quest_id == user_quest_id)
        .first()
    )

    if not user_quest:
        raise HTTPException(status_code=404, detail="사용자 퀘스트를 찾을 수 없습니다.")

    if user_quest.is_completed:
        return user_quest

    user_quest.is_completed = True
    user_quest.progress_value = user_quest.quest.target_value
    user_quest.completed_at = datetime.now()

    # 보상 지급
    currency = db.query(models.UserCurrency).filter(models.UserCurrency.user_id == user_quest.user_id).first()
    if currency:
        currency.gold += user_quest.quest.reward_gold
        currency.gem += user_quest.quest.reward_gem

    character_status = (
        db.query(models.CharacterStatus)
        .filter(models.CharacterStatus.user_id == user_quest.user_id)
        .first()
    )
    if character_status:
        character_status.exp += user_quest.quest.reward_exp

    db.commit()
    db.refresh(user_quest)
    return user_quest


@app.patch("/user-quests/{user_quest_id}/claim-reward/", response_model=schemas.UserQuestResponse)
def claim_user_quest_reward(user_quest_id: int, db: Session = Depends(get_db)):
    user_quest = get_or_404(
        db,
        models.UserQuest,
        models.UserQuest.user_quest_id == user_quest_id,
        "사용자 퀘스트를 찾을 수 없습니다.",
    )

    if not user_quest.is_completed:
        raise HTTPException(status_code=400, detail="아직 완료되지 않은 퀘스트입니다.")

    user_quest.is_reward_claimed = True
    db.commit()
    db.refresh(user_quest)
    return user_quest


# --------------------
# Phone Usage Logs
# --------------------

@app.post("/usage-logs/", response_model=schemas.PhoneUsageLogResponse, status_code=status.HTTP_201_CREATED)
def create_usage_log(log_data: schemas.PhoneUsageLogCreate, db: Session = Depends(get_db)):
    get_or_404(db, models.User, models.User.user_id == log_data.user_id, "사용자를 찾을 수 없습니다.")

    exists = (
        db.query(models.PhoneUsageLog)
        .filter(
            models.PhoneUsageLog.user_id == log_data.user_id,
            models.PhoneUsageLog.usage_date == log_data.usage_date,
        )
        .first()
    )

    if exists:
        raise HTTPException(status_code=409, detail="해당 날짜의 사용 기록이 이미 존재합니다.")

    usage_log = models.PhoneUsageLog(**log_data.model_dump())
    db.add(usage_log)
    db.commit()
    db.refresh(usage_log)
    return usage_log


@app.get("/users/{user_id}/usage-logs/", response_model=List[schemas.PhoneUsageLogResponse])
def get_user_usage_logs(user_id: int, db: Session = Depends(get_db)):
    get_or_404(db, models.User, models.User.user_id == user_id, "사용자를 찾을 수 없습니다.")

    return (
        db.query(models.PhoneUsageLog)
        .filter(models.PhoneUsageLog.user_id == user_id)
        .order_by(models.PhoneUsageLog.usage_date.desc())
        .all()
    )


# --------------------
# AI Feedback Logs
# --------------------

@app.post("/ai-feedbacks/", response_model=schemas.AIFeedbackResponse, status_code=status.HTTP_201_CREATED)
def create_ai_feedback(feedback_data: schemas.AIFeedbackCreate, db: Session = Depends(get_db)):
    get_or_404(db, models.User, models.User.user_id == feedback_data.user_id, "사용자를 찾을 수 없습니다.")

    if feedback_data.usage_log_id is not None:
        get_or_404(
            db,
            models.PhoneUsageLog,
            models.PhoneUsageLog.usage_log_id == feedback_data.usage_log_id,
            "사용시간 기록을 찾을 수 없습니다.",
        )

    feedback = models.AIFeedbackLog(**feedback_data.model_dump())
    db.add(feedback)
    db.commit()
    db.refresh(feedback)
    return feedback


@app.get("/users/{user_id}/ai-feedbacks/", response_model=List[schemas.AIFeedbackResponse])
def get_user_ai_feedbacks(user_id: int, db: Session = Depends(get_db)):
    get_or_404(db, models.User, models.User.user_id == user_id, "사용자를 찾을 수 없습니다.")

    return (
        db.query(models.AIFeedbackLog)
        .filter(models.AIFeedbackLog.user_id == user_id)
        .order_by(models.AIFeedbackLog.created_at.desc())
        .all()
    )


# --------------------
# Offline Reward Boxes
# --------------------

@app.post("/offline-reward-boxes/", response_model=schemas.OfflineRewardBoxResponse, status_code=status.HTTP_201_CREATED)
def create_offline_reward_box(box_data: schemas.OfflineRewardBoxCreate, db: Session = Depends(get_db)):
    get_or_404(db, models.User, models.User.user_id == box_data.user_id, "사용자를 찾을 수 없습니다.")

    box = models.OfflineRewardBox(**box_data.model_dump())
    db.add(box)
    db.commit()
    db.refresh(box)
    return box


@app.get("/users/{user_id}/offline-reward-boxes/", response_model=List[schemas.OfflineRewardBoxResponse])
def get_user_offline_reward_boxes(user_id: int, db: Session = Depends(get_db)):
    get_or_404(db, models.User, models.User.user_id == user_id, "사용자를 찾을 수 없습니다.")

    return (
        db.query(models.OfflineRewardBox)
        .filter(models.OfflineRewardBox.user_id == user_id)
        .order_by(models.OfflineRewardBox.created_at.desc())
        .all()
    )


@app.patch("/offline-reward-boxes/{reward_box_id}", response_model=schemas.OfflineRewardBoxResponse)
def update_offline_reward_box(
    reward_box_id: int,
    box_update: schemas.OfflineRewardBoxUpdate,
    db: Session = Depends(get_db),
):
    box = get_or_404(
        db,
        models.OfflineRewardBox,
        models.OfflineRewardBox.reward_box_id == reward_box_id,
        "오프라인 보상 상자를 찾을 수 없습니다.",
    )

    apply_update(box, box_update)

    if box.is_claimed and box.claimed_at is None:
        box.claimed_at = datetime.now()

    db.commit()
    db.refresh(box)
    return box


@app.patch("/offline-reward-boxes/{reward_box_id}/claim/", response_model=schemas.OfflineRewardBoxResponse)
def claim_offline_reward_box(reward_box_id: int, db: Session = Depends(get_db)):
    box = get_or_404(
        db,
        models.OfflineRewardBox,
        models.OfflineRewardBox.reward_box_id == reward_box_id,
        "오프라인 보상 상자를 찾을 수 없습니다.",
    )

    if box.is_claimed:
        return box

    currency = db.query(models.UserCurrency).filter(models.UserCurrency.user_id == box.user_id).first()
    if currency:
        currency.gold += box.reward_gold

    character_status = db.query(models.CharacterStatus).filter(models.CharacterStatus.user_id == box.user_id).first()
    if character_status:
        character_status.exp += box.reward_exp
        character_status.total_boss_kill_count += box.boss_kill_count

    box.is_claimed = True
    box.claimed_at = datetime.now()

    db.commit()
    db.refresh(box)
    return box
