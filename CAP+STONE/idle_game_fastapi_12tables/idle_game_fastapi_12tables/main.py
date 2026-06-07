import json
import os
from datetime import date, timedelta, datetime
from typing import List

from fastapi import Depends, FastAPI, HTTPException, status
from google import genai
from pydantic import BaseModel
from sqlalchemy import func, text
from sqlalchemy.exc import IntegrityError
from sqlalchemy.orm import Session, joinedload

from routes import battle

import models
import schemas
from database import get_db
from sqlalchemy import desc

app = FastAPI(title="Idle Game API")
app.include_router(battle.router)


# ======================================================
# Common Utils
# ======================================================

def get_or_404(db: Session, model, condition, detail: str):
    obj = db.query(model).filter(condition).first()
    if obj is None:
        raise HTTPException(status_code=404, detail=detail)
    return obj


def apply_update(db_obj, update_data):
    data = update_data.model_dump(exclude_unset=True)
    for key, value in data.items():
        setattr(db_obj, key, value)


def condition_grade_to_score(grade: str) -> int:
    """
    user_buff.condition_score는 0~100 기준.
    NORMAL / GOOD / BEST를 대표 점수로 변환.
    """
    if grade == "BEST":
        return 100
    if grade == "GOOD":
        return 70
    return 30


def calculate_required_exp(level: int) -> int:
    """
    플레이어 레벨업 필요 경험치:
    1레벨 기준 1000,
    레벨당 16% 증가.
    """
    level = max(1, level)
    return int(1000 * (1.16 ** (level - 1)))


EQUIPMENT_ENHANCE_LEVEL_MIN = 1
EQUIPMENT_ENHANCE_GOLD_BASE = 500
EQUIPMENT_ENHANCE_GOLD_RATE = 1.18

EQUIPMENT_REQUIRED_ITEM_BASE = 2


def calculate_enhance_gold(enhance_level: int) -> int:
    """
    장비 강화 골드 비용.

    장비 레벨은 1부터 시작.
    Lv.1 -> Lv.2 비용: 500 × 1.18^0 = 500
    Lv.2 -> Lv.3 비용: 500 × 1.18^1 = 590
    """
    safe_level = max(EQUIPMENT_ENHANCE_LEVEL_MIN, enhance_level)
    n = safe_level - 1

    return int(round(EQUIPMENT_ENHANCE_GOLD_BASE * (EQUIPMENT_ENHANCE_GOLD_RATE ** n)))


def calculate_required_item_count(enhance_level: int) -> int:
    """
    장비 강화 필요 같은 장비 수.

    장비 레벨은 1부터 시작.
    Lv.1 -> Lv.2: 2개
    Lv.2 -> Lv.3: 3개
    Lv.3 -> Lv.4: 4개
    """
    safe_level = max(EQUIPMENT_ENHANCE_LEVEL_MIN, enhance_level)
    n = safe_level - 1

    return EQUIPMENT_REQUIRED_ITEM_BASE + n


def calculate_equipment_main_effect(base_value: int, enhance_level: int) -> int:
    """
    장비 main effect.

    공식:
    기본 수치 × 1.15^n

    Lv.1이면 n = 0
    """
    safe_level = max(EQUIPMENT_ENHANCE_LEVEL_MIN, enhance_level)
    n = safe_level - 1

    return int(round(base_value * (1.15 ** n)))


def calculate_equipment_sub_effect(base_rate: float, enhance_level: int) -> float:
    """
    장비 sub effect.

    공식:
    기본 퍼센트 × 1.05^n

    Lv.1이면 n = 0
    """
    safe_level = max(EQUIPMENT_ENHANCE_LEVEL_MIN, enhance_level)
    n = safe_level - 1

    return base_rate * (1.05 ** n)


def calculate_equipment_attack(base_attack: int, enhance_level: int) -> int:
    return calculate_equipment_main_effect(base_attack, enhance_level)


def calculate_equipment_defense(base_defense: int, enhance_level: int) -> int:
    return calculate_equipment_main_effect(base_defense, enhance_level)


PLAYER_BASE_HP = 100
PLAYER_BASE_ATTACK = 20
PLAYER_BASE_DEFENSE = 20

STAT_UPGRADE_COST_BASE = 1000
STAT_UPGRADE_COST_RATE = 1.17

STAT_UPGRADE_AMOUNT_BASE = 1
STAT_UPGRADE_AMOUNT_RATE = 1.13


def calculate_user_stat_upgrade_cost(upgrade_lvl: int) -> int:
    upgrade_lvl = max(1, upgrade_lvl)
    n = upgrade_lvl - 1
    return int(STAT_UPGRADE_COST_BASE * (STAT_UPGRADE_COST_RATE ** n))


def calculate_stat_upgrade_amount(upgrade_lvl: int) -> int:
    upgrade_lvl = max(1, upgrade_lvl)
    n = upgrade_lvl - 1
    return max(1, int(STAT_UPGRADE_AMOUNT_BASE * (STAT_UPGRADE_AMOUNT_RATE ** n)))


def calculate_hp_upgrade_amount(upgrade_lvl: int) -> int:
    return calculate_stat_upgrade_amount(upgrade_lvl)


def calculate_attack_upgrade_amount(upgrade_lvl: int) -> int:
    return calculate_stat_upgrade_amount(upgrade_lvl)


def calculate_defense_upgrade_amount(upgrade_lvl: int) -> int:
    return calculate_stat_upgrade_amount(upgrade_lvl)


def decide_condition_result(
    today_minutes: int,
    average_minutes: float,
    previous_condition_quest_completed: int,
) -> str:
    if average_minutes <= 0:
        usage_grade_score = 2
    else:
        change_rate = ((average_minutes - today_minutes) / average_minutes) * 100

        if change_rate < -5:
            usage_grade_score = 1
        elif -5 <= change_rate < 5:
            usage_grade_score = 2
        else:
            usage_grade_score = 3

    if previous_condition_quest_completed <= 1:
        quest_grade_score = 1
    elif previous_condition_quest_completed <= 3:
        quest_grade_score = 2
    else:
        quest_grade_score = 3

    final_score = usage_grade_score

    if final_score >= 3:
        return "BEST"
    if final_score == 2:
        return "GOOD"
    return "NORMAL"


def get_condition_label(condition_result: str) -> str:
    if condition_result == "BEST":
        return "상"
    if condition_result == "GOOD":
        return "중"
    return "하"


def parse_gemini_json(raw_text: str) -> dict:
    cleaned = raw_text.strip()
    cleaned = cleaned.replace("```json", "").replace("```", "").strip()

    try:
        return json.loads(cleaned)
    except json.JSONDecodeError:
        return {
            "feedback_content": cleaned,
            "pattern_summary": "AI 응답을 JSON으로 파싱하지 못해 원문을 저장했습니다.",
        }


def call_gemini_for_feedback(
    total_screen_minutes: int,
    average_minutes: float,
    previous_condition_quest_completed: int,
    condition_result: str,
):
    api_key = os.getenv("GEMINI_API_KEY")

    if not api_key:
        raise HTTPException(
            status_code=500,
            detail="GEMINI_API_KEY가 .env에 설정되어 있지 않습니다.",
        )

    condition_label = get_condition_label(condition_result)

    prompt = f"""
너는 사용자의 휴대폰 사용 습관 개선을 돕는 방치형 게임의 AI 피드백 시스템이다.

분석 데이터:
- 오늘 휴대폰 사용시간: {total_screen_minutes}분
- 최근 평균 휴대폰 사용시간: {average_minutes:.1f}분
- 전날 컨디션 측정용 퀘스트 완료 개수: {previous_condition_quest_completed}개
- 서버 판정 컨디션 등급: {condition_result}
- 사용자 표시용 등급: {condition_label}

등급 기준:
- NORMAL = 하
- GOOD = 중
- BEST = 상

응답 규칙:
- JSON 형식으로만 답변한다.
- 마크다운 코드블록을 쓰지 않는다.
- JSON 밖에 설명 문장을 쓰지 않는다.
- condition_result는 이미 서버에서 결정했으므로 바꾸지 않는다.
- feedback_content는 사용자에게 보여줄 짧은 피드백 문장이다.
- pattern_summary는 사용시간 변화와 전날 퀘스트 완료 개수를 요약한다.

응답 형식:
{{
  "feedback_content": "string",
  "pattern_summary": "string"
}}
"""

    client = genai.Client(api_key=api_key)

    response = client.models.generate_content(
        model="gemini-2.5-flash",
        contents=prompt,
    )

    result = parse_gemini_json(response.text)

    return {
        "feedback_content": result.get(
            "feedback_content",
            "오늘의 사용 패턴 분석 결과를 생성했습니다.",
        ),
        "pattern_summary": result.get(
            "pattern_summary",
            f"오늘 {total_screen_minutes}분 사용, 최근 평균 {average_minutes:.1f}분, 전날 퀘스트 {previous_condition_quest_completed}개 완료",
        ),
        "condition_result": condition_result,
    }


def create_user_daily_buffs(db: Session, user_id: int, condition_result: str = "BEST"):
    """
    user_buff는 유저별/날짜별/버프타입별 상태 테이블.
    초기 유저 생성 시 오늘 날짜 기준 4종 버프를 생성한다.

    buff_info는 ACTIVITY / RESTRAINT / QUEST / OFFLINE
    각 타입별 NORMAL / GOOD / BEST 데이터를 갖고 있어야 한다.
    """
    condition_score = condition_grade_to_score(condition_result)

    buff_count = db.execute(
        text(
            """
            SELECT COUNT(*)
            FROM buff_info
            WHERE condition_grade = :condition_grade
            """
        ),
        {"condition_grade": condition_result},
    ).scalar()

    if buff_count != 4:
        raise HTTPException(
            status_code=400,
            detail=f"buff_info에 {condition_result} 등급 버프가 4개 필요합니다. 현재 개수: {buff_count}",
        )

    db.execute(
        text(
            """
            INSERT INTO user_buff (
                user_id,
                buff_id,
                buff_type,
                condition_score,
                current_effect_value,
                buff_date,
                is_active
            )
            SELECT
                :user_id,
                bi.buff_id,
                bi.buff_type,
                :condition_score,
                bi.effect_value,
                CURRENT_DATE,
                1
            FROM buff_info bi
            WHERE bi.condition_grade = :condition_grade
            ON DUPLICATE KEY UPDATE
                buff_id = VALUES(buff_id),
                condition_score = VALUES(condition_score),
                current_effect_value = VALUES(current_effect_value),
                is_active = VALUES(is_active),
                updated_at = CURRENT_TIMESTAMP
            """
        ),
        {
            "user_id": user_id,
            "condition_score": condition_score,
            "condition_grade": condition_result,
        },
    )


# ======================================================
# Health
# ======================================================

@app.get("/health")
def health_check():
    return {"status": "ok"}


# ======================================================
# Character Info
# ======================================================

@app.post(
    "/characters/",
    response_model=schemas.CharacterInfoResponse,
    status_code=status.HTTP_201_CREATED,
)
def create_character(
    character_data: schemas.CharacterInfoCreate,
    db: Session = Depends(get_db),
):
    character = models.CharacterInfo(**character_data.model_dump())

    try:
        db.add(character)
        db.commit()
        db.refresh(character)
        return character
    except IntegrityError:
        db.rollback()
        raise HTTPException(
            status_code=409,
            detail="이미 존재하는 character_key입니다.",
        )


@app.get("/characters/", response_model=List[schemas.CharacterInfoResponse])
def get_characters(db: Session = Depends(get_db)):
    return (
        db.query(models.CharacterInfo)
        .order_by(models.CharacterInfo.character_id)
        .all()
    )


@app.patch(
    "/characters/{character_id}",
    response_model=schemas.CharacterInfoResponse,
)
def update_character(
    character_id: int,
    update_data: schemas.CharacterInfoUpdate,
    db: Session = Depends(get_db),
):
    character = get_or_404(
        db,
        models.CharacterInfo,
        models.CharacterInfo.character_id == character_id,
        "캐릭터를 찾을 수 없습니다.",
    )

    apply_update(character, update_data)

    try:
        db.commit()
        db.refresh(character)
        return character
    except IntegrityError:
        db.rollback()
        raise HTTPException(
            status_code=409,
            detail="이미 존재하는 character_key입니다.",
        )


# ======================================================
# Users
# ======================================================

@app.post(
    "/users/",
    response_model=schemas.UserResponse,
    status_code=status.HTTP_201_CREATED,
)
def create_user(user_data: schemas.UserCreate, db: Session = Depends(get_db)):
    default_character = get_or_404(
        db,
        models.CharacterInfo,
        models.CharacterInfo.character_id == user_data.default_character_id,
        "기본 캐릭터를 찾을 수 없습니다.",
    )

    exists_email = (
        db.query(models.User)
        .filter(models.User.email == str(user_data.email))
        .first()
    )

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

        db.add(
            models.UserStatus(
                user_id=user.user_id,
                current_character_id=default_character.character_id,

                player_level=1,
                player_exp=0,
                required_exp=1000,

                current_stage=1,
                total_boss_kill_count=0,

                max_hp=100,
                attack_power=20,
                defense_power=20,

                hp_upgrade_lvl=1,
                attack_upgrade_lvl=1,
                defense_upgrade_lvl=1,
            )
        )

        db.add(
            models.UserCurrency(
                user_id=user.user_id,
                gold=0,
                gem=0,
            )
        )

        characters = db.query(models.CharacterInfo).all()

        if len(characters) == 0:
            raise HTTPException(
                status_code=400,
                detail="character_info에 등록된 캐릭터가 없습니다.",
            )

        for character in characters:
            db.add(
                models.CharacterStatus(
                    user_id=user.user_id,
                    character_id=character.character_id,
                )
            )

        create_user_daily_buffs(
            db=db,
            user_id=user.user_id,
            condition_result="BEST",
        )

        db.commit()
        db.refresh(user)
        return user

    except HTTPException:
        db.rollback()
        raise

    except IntegrityError:
        db.rollback()
        raise HTTPException(
            status_code=400,
            detail="사용자 생성 중 무결성 오류가 발생했습니다.",
        )


@app.get("/users/", response_model=List[schemas.UserResponse])
def get_users(skip: int = 0, limit: int = 100, db: Session = Depends(get_db)):
    return (
        db.query(models.User)
        .options(joinedload(models.User.status))
        .offset(skip)
        .limit(limit)
        .all()
    )


@app.get("/users/{user_id}", response_model=schemas.UserResponse)
def get_user(user_id: int, db: Session = Depends(get_db)):
    return get_or_404(
        db,
        models.User,
        models.User.user_id == user_id,
        "사용자를 찾을 수 없습니다.",
    )


@app.patch("/users/{user_id}", response_model=schemas.UserResponse)
def update_user(
    user_id: int,
    user_update: schemas.UserUpdate,
    db: Session = Depends(get_db),
):
    user = get_or_404(
        db,
        models.User,
        models.User.user_id == user_id,
        "사용자를 찾을 수 없습니다.",
    )

    apply_update(user, user_update)

    try:
        db.commit()
        db.refresh(user)
        return user
    except IntegrityError:
        db.rollback()
        raise HTTPException(status_code=409, detail="이미 사용 중인 이메일입니다.")


@app.get("/users/{user_id}/status")
def get_user_status(user_id: int, db: Session = Depends(get_db)):
    status_row = db.execute(
        text(
            """
            SELECT
                us.user_id,
                us.player_level AS level,
                us.player_exp AS exp,
                us.required_exp,
                COALESCE(uc.gem, 0) AS gem
            FROM user_status us
            LEFT JOIN user_currency uc
                ON us.user_id = uc.user_id
            WHERE us.user_id = :user_id
            """
        ),
        {"user_id": user_id},
    ).mappings().first()

    if status_row is None:
        raise HTTPException(
            status_code=404,
            detail="User status not found",
        )

    return dict(status_row)


@app.patch(
    "/users/{user_id}/current-character/{character_id}",
    response_model=schemas.UserStatusResponse,
)
def change_current_character(
    user_id: int,
    character_id: int,
    db: Session = Depends(get_db),
):
    user_status = get_or_404(
        db,
        models.UserStatus,
        models.UserStatus.user_id == user_id,
        "유저 상태 정보를 찾을 수 없습니다.",
    )

    get_or_404(
        db,
        models.CharacterInfo,
        models.CharacterInfo.character_id == character_id,
        "캐릭터를 찾을 수 없습니다.",
    )

    get_or_404(
        db,
        models.CharacterStatus,
        (models.CharacterStatus.user_id == user_id)
        & (models.CharacterStatus.character_id == character_id),
        "해당 유저가 보유하지 않은 캐릭터입니다.",
    )

    user_status.current_character_id = character_id

    db.commit()
    db.refresh(user_status)
    return user_status


# ======================================================
# Character Status
# ======================================================

@app.get(
    "/users/{user_id}/character-statuses/",
    response_model=List[schemas.CharacterStatusResponse],
)
def get_user_character_statuses(user_id: int, db: Session = Depends(get_db)):
    return (
        db.query(models.CharacterStatus)
        .options(joinedload(models.CharacterStatus.character))
        .filter(models.CharacterStatus.user_id == user_id)
        .all()
    )


@app.patch(
    "/users/{user_id}/characters/{character_id}/status/",
    response_model=schemas.CharacterStatusResponse,
)
def update_character_status(
    user_id: int,
    character_id: int,
    status_update: schemas.CharacterStatusUpdate,
    db: Session = Depends(get_db),
):
    character_status = get_or_404(
        db,
        models.CharacterStatus,
        (models.CharacterStatus.user_id == user_id)
        & (models.CharacterStatus.character_id == character_id),
        "캐릭터 상태를 찾을 수 없습니다.",
    )

    apply_update(character_status, status_update)
    db.commit()
    db.refresh(character_status)
    return character_status


# ======================================================
# Items
# ======================================================

@app.post(
    "/items/",
    response_model=schemas.ItemResponse,
    status_code=status.HTTP_201_CREATED,
)
def create_item(item_data: schemas.ItemCreate, db: Session = Depends(get_db)):
    item = models.Item(**item_data.model_dump())

    try:
        db.add(item)
        db.commit()
        db.refresh(item)
        return item
    except IntegrityError:
        db.rollback()
        raise HTTPException(status_code=409, detail="이미 존재하는 item_key입니다.")


@app.get("/items/", response_model=List[schemas.ItemResponse])
def get_items(db: Session = Depends(get_db)):
    return db.query(models.Item).order_by(models.Item.item_id).all()


@app.patch("/items/{item_id}", response_model=schemas.ItemResponse)
def update_item(
    item_id: int,
    item_update: schemas.ItemUpdate,
    db: Session = Depends(get_db),
):
    item = get_or_404(
        db,
        models.Item,
        models.Item.item_id == item_id,
        "아이템을 찾을 수 없습니다.",
    )

    apply_update(item, item_update)

    try:
        db.commit()
        db.refresh(item)
        return item
    except IntegrityError:
        db.rollback()
        raise HTTPException(status_code=409, detail="이미 존재하는 item_key입니다.")


# ======================================================
# User Items
# ======================================================

@app.post(
    "/user-items/",
    response_model=schemas.UserItemResponse,
    status_code=status.HTTP_201_CREATED,
)
def create_user_item(
    user_item_data: schemas.UserItemCreate,
    db: Session = Depends(get_db),
):
    get_or_404(
        db,
        models.User,
        models.User.user_id == user_item_data.user_id,
        "사용자를 찾을 수 없습니다.",
    )

    get_or_404(
        db,
        models.Item,
        models.Item.item_id == user_item_data.item_id,
        "아이템을 찾을 수 없습니다.",
    )

    safe_enhance_level = max(
        EQUIPMENT_ENHANCE_LEVEL_MIN,
        user_item_data.enhance_level,
    )

    existing_user_item = (
        db.query(models.UserItem)
        .filter(
            models.UserItem.user_id == user_item_data.user_id,
            models.UserItem.item_id == user_item_data.item_id,
        )
        .first()
    )

    if existing_user_item:
        existing_user_item.quantity += user_item_data.quantity

        if existing_user_item.enhance_level is None or existing_user_item.enhance_level < EQUIPMENT_ENHANCE_LEVEL_MIN:
            existing_user_item.enhance_level = EQUIPMENT_ENHANCE_LEVEL_MIN

        db.commit()
        db.refresh(existing_user_item)
        return existing_user_item

    user_item = models.UserItem(
        user_id=user_item_data.user_id,
        item_id=user_item_data.item_id,
        quantity=user_item_data.quantity,
        enhance_level=safe_enhance_level,
        is_equipped=user_item_data.is_equipped,
    )

    db.add(user_item)
    db.commit()
    db.refresh(user_item)
    return user_item


@app.get(
    "/users/{user_id}/items/",
    response_model=List[schemas.UserItemResponse],
)
def get_user_items(user_id: int, db: Session = Depends(get_db)):
    user_items = (
        db.query(models.UserItem)
        .options(joinedload(models.UserItem.item))
        .filter(models.UserItem.user_id == user_id)
        .all()
    )

    for user_item in user_items:
        if user_item.enhance_level is None or user_item.enhance_level < EQUIPMENT_ENHANCE_LEVEL_MIN:
            user_item.enhance_level = EQUIPMENT_ENHANCE_LEVEL_MIN

        if user_item.quantity is None or user_item.quantity < 0:
            user_item.quantity = 0

    return user_items


@app.patch(
    "/user-items/{user_item_id}",
    response_model=schemas.UserItemResponse,
)
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

    update_data = user_item_update.model_dump(exclude_unset=True)

    if "enhance_level" in update_data and update_data["enhance_level"] is not None:
        update_data["enhance_level"] = max(
            EQUIPMENT_ENHANCE_LEVEL_MIN,
            update_data["enhance_level"],
        )

    if "quantity" in update_data and update_data["quantity"] is not None:
        update_data["quantity"] = max(0, update_data["quantity"])

    for key, value in update_data.items():
        setattr(user_item, key, value)

    db.commit()
    db.refresh(user_item)
    return user_item


@app.patch(
    "/user-items/{user_item_id}/equip/",
    response_model=schemas.UserItemResponse,
)
def equip_user_item(user_item_id: int, db: Session = Depends(get_db)):
    user_item = (
        db.query(models.UserItem)
        .options(joinedload(models.UserItem.item))
        .filter(models.UserItem.user_item_id == user_item_id)
        .first()
    )

    if user_item is None:
        raise HTTPException(
            status_code=404,
            detail="보유 아이템을 찾을 수 없습니다.",
        )

    if user_item.item is None:
        raise HTTPException(
            status_code=404,
            detail="아이템 정보를 찾을 수 없습니다.",
        )

    target_item_type = user_item.item.item_type

    same_type_items = (
        db.query(models.UserItem)
        .join(models.Item, models.UserItem.item_id == models.Item.item_id)
        .filter(
            models.UserItem.user_id == user_item.user_id,
            models.Item.item_type == target_item_type,
        )
        .all()
    )

    for item in same_type_items:
        item.is_equipped = False

    user_item.is_equipped = True

    db.commit()
    db.refresh(user_item)
    return user_item


@app.patch(
    "/user-items/{user_item_id}/enhance/",
    response_model=schemas.UserItemResponse,
)
def enhance_user_item(user_item_id: int, db: Session = Depends(get_db)):
    user_item = (
        db.query(models.UserItem)
        .options(joinedload(models.UserItem.item))
        .filter(models.UserItem.user_item_id == user_item_id)
        .first()
    )

    if user_item is None:
        raise HTTPException(
            status_code=404,
            detail="보유 아이템을 찾을 수 없습니다.",
        )

    if user_item.enhance_level is None or user_item.enhance_level < EQUIPMENT_ENHANCE_LEVEL_MIN:
        user_item.enhance_level = EQUIPMENT_ENHANCE_LEVEL_MIN

    if user_item.quantity is None or user_item.quantity < 0:
        user_item.quantity = 0


    currency = get_or_404(
        db,
        models.UserCurrency,
        models.UserCurrency.user_id == user_item.user_id,
        "재화 정보를 찾을 수 없습니다.",
    )

    required_count = calculate_required_item_count(user_item.enhance_level)
    required_gold = calculate_enhance_gold(user_item.enhance_level)

    if user_item.quantity < required_count:
        raise HTTPException(
            status_code=400,
            detail=f"강화 재료가 부족합니다. 필요 수량: {required_count}, 보유 수량: {user_item.quantity}",
        )

    if currency.gold < required_gold:
        raise HTTPException(
            status_code=400,
            detail=f"골드가 부족합니다. 필요 골드: {required_gold}, 보유 골드: {currency.gold}",
        )

    user_item.quantity -= required_count
    user_item.enhance_level += 1


    currency.gold -= required_gold
    currency.updated_at = datetime.now()

    db.commit()
    db.refresh(user_item)
    db.refresh(currency)

    return user_item


# ======================================================
# Currency
# ======================================================

@app.get(
    "/users/{user_id}/currency/",
    response_model=schemas.UserCurrencyResponse,
)
def get_user_currency(user_id: int, db: Session = Depends(get_db)):
    return get_or_404(
        db,
        models.UserCurrency,
        models.UserCurrency.user_id == user_id,
        "재화 정보를 찾을 수 없습니다.",
    )


@app.patch(
    "/users/{user_id}/currency/",
    response_model=schemas.UserCurrencyResponse,
)
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


@app.patch("/users/{user_id}/currency/spend-gold/")
def spend_gold(
    user_id: int,
    request: schemas.SpendCurrencyRequest,
    db: Session = Depends(get_db),
):
    if request.amount <= 0:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail="차감할 gold 수량이 올바르지 않습니다.",
        )

    currency = get_or_404(
        db,
        models.UserCurrency,
        models.UserCurrency.user_id == user_id,
        "재화 정보를 찾을 수 없습니다.",
    )

    if currency.gold < request.amount:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail="gold가 부족합니다.",
        )

    currency.gold -= request.amount
    currency.updated_at = datetime.now()

    db.commit()
    db.refresh(currency)

    return {
        "user_id": user_id,
        "gold": currency.gold,
        "gem": currency.gem,
        "updated_at": currency.updated_at,
    }


@app.patch("/users/{user_id}/currency/spend-gem/")
def spend_gem(
    user_id: int,
    request: schemas.SpendCurrencyRequest,
    db: Session = Depends(get_db),
):
    if request.amount <= 0:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail="차감할 gem 수량이 올바르지 않습니다.",
        )

    currency = get_or_404(
        db,
        models.UserCurrency,
        models.UserCurrency.user_id == user_id,
        "재화 정보를 찾을 수 없습니다.",
    )

    if currency.gem < request.amount:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail="gem이 부족합니다.",
        )

    currency.gem -= request.amount
    currency.updated_at = datetime.now()

    db.commit()
    db.refresh(currency)

    return {
        "user_id": user_id,
        "gold": currency.gold,
        "gem": currency.gem,
        "updated_at": currency.updated_at,
    }


# ======================================================
# User Stat Upgrade
# ======================================================

@app.patch(
    "/users/{user_id}/status/upgrade-attack/",
    response_model=schemas.UserStatUpgradeResponse,
)
def upgrade_user_attack(
    user_id: int,
    db: Session = Depends(get_db),
):
    user_status = get_or_404(
        db,
        models.UserStatus,
        models.UserStatus.user_id == user_id,
        "유저 상태 정보를 찾을 수 없습니다.",
    )

    currency = get_or_404(
        db,
        models.UserCurrency,
        models.UserCurrency.user_id == user_id,
        "재화 정보를 찾을 수 없습니다.",
    )

    cost_gold = calculate_user_stat_upgrade_cost(user_status.attack_upgrade_lvl)

    if currency.gold < cost_gold:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=f"골드가 부족합니다. 필요 골드: {cost_gold}, 보유 골드: {currency.gold}",
        )

    increase_attack = calculate_attack_upgrade_amount(user_status.attack_upgrade_lvl)

    currency.gold -= cost_gold
    user_status.attack_power += increase_attack
    user_status.attack_upgrade_lvl += 1

    user_status.updated_at = datetime.now()
    currency.updated_at = datetime.now()

    db.commit()
    db.refresh(user_status)
    db.refresh(currency)

    return {
        "user_id": user_id,
        "upgrade_type": "ATTACK",

        "max_hp": user_status.max_hp,
        "attack_power": user_status.attack_power,
        "defense_power": user_status.defense_power,

        "hp_upgrade_lvl": user_status.hp_upgrade_lvl,
        "attack_upgrade_lvl": user_status.attack_upgrade_lvl,
        "defense_upgrade_lvl": user_status.defense_upgrade_lvl,

        "upgrade_lvl": user_status.attack_upgrade_lvl,

        "gold": currency.gold,
        "cost_gold": cost_gold,
    }


@app.patch(
    "/users/{user_id}/status/upgrade-hp/",
    response_model=schemas.UserStatUpgradeResponse,
)
def upgrade_user_hp(
    user_id: int,
    db: Session = Depends(get_db),
):
    user_status = get_or_404(
        db,
        models.UserStatus,
        models.UserStatus.user_id == user_id,
        "유저 상태 정보를 찾을 수 없습니다.",
    )

    currency = get_or_404(
        db,
        models.UserCurrency,
        models.UserCurrency.user_id == user_id,
        "재화 정보를 찾을 수 없습니다.",
    )

    cost_gold = calculate_user_stat_upgrade_cost(user_status.hp_upgrade_lvl)

    if currency.gold < cost_gold:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=f"골드가 부족합니다. 필요 골드: {cost_gold}, 보유 골드: {currency.gold}",
        )

    increase_hp = calculate_hp_upgrade_amount(user_status.hp_upgrade_lvl)

    currency.gold -= cost_gold
    user_status.max_hp += increase_hp
    user_status.hp_upgrade_lvl += 1

    user_status.updated_at = datetime.now()
    currency.updated_at = datetime.now()

    db.commit()
    db.refresh(user_status)
    db.refresh(currency)

    return {
        "user_id": user_id,
        "upgrade_type": "HP",

        "max_hp": user_status.max_hp,
        "attack_power": user_status.attack_power,
        "defense_power": user_status.defense_power,

        "hp_upgrade_lvl": user_status.hp_upgrade_lvl,
        "attack_upgrade_lvl": user_status.attack_upgrade_lvl,
        "defense_upgrade_lvl": user_status.defense_upgrade_lvl,

        "upgrade_lvl": user_status.hp_upgrade_lvl,

        "gold": currency.gold,
        "cost_gold": cost_gold,
    }


@app.patch(
    "/users/{user_id}/status/upgrade-defense/",
    response_model=schemas.UserStatUpgradeResponse,
)
def upgrade_user_defense(
    user_id: int,
    db: Session = Depends(get_db),
):
    user_status = get_or_404(
        db,
        models.UserStatus,
        models.UserStatus.user_id == user_id,
        "유저 상태 정보를 찾을 수 없습니다.",
    )

    currency = get_or_404(
        db,
        models.UserCurrency,
        models.UserCurrency.user_id == user_id,
        "재화 정보를 찾을 수 없습니다.",
    )

    cost_gold = calculate_user_stat_upgrade_cost(user_status.defense_upgrade_lvl)

    if currency.gold < cost_gold:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=f"골드가 부족합니다. 필요 골드: {cost_gold}, 보유 골드: {currency.gold}",
        )

    increase_defense = calculate_defense_upgrade_amount(user_status.defense_upgrade_lvl)

    currency.gold -= cost_gold
    user_status.defense_power += increase_defense
    user_status.defense_upgrade_lvl += 1

    user_status.updated_at = datetime.now()
    currency.updated_at = datetime.now()

    db.commit()
    db.refresh(user_status)
    db.refresh(currency)

    return {
        "user_id": user_id,
        "upgrade_type": "DEFENSE",

        "max_hp": user_status.max_hp,
        "attack_power": user_status.attack_power,
        "defense_power": user_status.defense_power,

        "hp_upgrade_lvl": user_status.hp_upgrade_lvl,
        "attack_upgrade_lvl": user_status.attack_upgrade_lvl,
        "defense_upgrade_lvl": user_status.defense_upgrade_lvl,

        "upgrade_lvl": user_status.defense_upgrade_lvl,

        "gold": currency.gold,
        "cost_gold": cost_gold,
    }


# ======================================================
# Usage Logs
# ======================================================

@app.post(
    "/usage-logs/",
    response_model=schemas.PhoneUsageLogResponse,
    status_code=status.HTTP_201_CREATED,
)
def create_or_update_usage_log(
    usage_data: schemas.PhoneUsageLogCreate,
    db: Session = Depends(get_db),
):
    get_or_404(
        db,
        models.User,
        models.User.user_id == usage_data.user_id,
        "사용자를 찾을 수 없습니다.",
    )

    existing = (
        db.query(models.PhoneUsageLog)
        .filter(
            models.PhoneUsageLog.user_id == usage_data.user_id,
            models.PhoneUsageLog.usage_date == usage_data.usage_date,
        )
        .first()
    )

    if existing:
        existing.total_screen_minutes = usage_data.total_screen_minutes
        db.commit()
        db.refresh(existing)
        return existing

    usage_log = models.PhoneUsageLog(**usage_data.model_dump())

    db.add(usage_log)
    db.commit()
    db.refresh(usage_log)
    return usage_log


@app.get(
    "/users/{user_id}/usage-logs/",
    response_model=List[schemas.PhoneUsageLogResponse],
)
def get_user_usage_logs(user_id: int, db: Session = Depends(get_db)):
    return (
        db.query(models.PhoneUsageLog)
        .filter(models.PhoneUsageLog.user_id == user_id)
        .order_by(models.PhoneUsageLog.usage_date.desc())
        .all()
    )


# ======================================================
# Quests
# ======================================================

class QuestProgressRequest(BaseModel):
    quest_event: str
    add_value: int = 1


@app.post(
    "/quests/",
    response_model=schemas.QuestResponse,
    status_code=status.HTTP_201_CREATED,
)
def create_quest(
    quest_data: schemas.QuestCreate,
    db: Session = Depends(get_db),
):
    quest = models.Quest(**quest_data.model_dump())

    db.add(quest)
    db.commit()
    db.refresh(quest)

    return quest


@app.get("/quests/", response_model=List[schemas.QuestResponse])
def get_quests(db: Session = Depends(get_db)):
    return (
        db.query(models.Quest)
        .order_by(models.Quest.quest_id.asc())
        .all()
    )


@app.patch(
    "/quests/{quest_id}",
    response_model=schemas.QuestResponse,
)
def update_quest(
    quest_id: int,
    quest_update: schemas.QuestUpdate,
    db: Session = Depends(get_db),
):
    quest = get_or_404(
        db,
        models.Quest,
        models.Quest.quest_id == quest_id,
        "퀘스트를 찾을 수 없습니다.",
    )

    update_data = quest_update.model_dump(exclude_unset=True)

    for key, value in update_data.items():
        setattr(quest, key, value)

    db.commit()
    db.refresh(quest)

    return quest


@app.get("/quests/today/{user_id}")
def get_today_quests_for_unity(
    user_id: int,
    db: Session = Depends(get_db),
):
    get_or_404(
        db,
        models.User,
        models.User.user_id == user_id,
        "사용자를 찾을 수 없습니다.",
    )

    today_quests = get_today_user_quests(db, user_id)

    if len(today_quests) == 0:
        condition_result = get_latest_condition_result(db, user_id)

        assign_today_quests_by_condition(
            db=db,
            user_id=user_id,
            condition_result=condition_result,
            limit=4,
        )

        db.commit()
        today_quests = get_today_user_quests(db, user_id)

    refresh_quest_bonus_progress(db, user_id)
    db.commit()

    today_quests = get_today_user_quests(db, user_id)

    return {
        "success": True,
        "user_id": user_id,
        "quests": [
            serialize_user_quest(user_quest)
            for user_quest in today_quests
        ],
    }


@app.patch("/quests/progress/{user_id}")
def update_quest_progress(
    user_id: int,
    request: schemas.QuestProgressRequest,
    db: Session = Depends(get_db),
):
    user = db.query(models.User).filter(models.User.user_id == user_id).first()

    if user is None:
        raise HTTPException(status_code=404, detail="유저를 찾을 수 없습니다.")

    if request.add_value <= 0:
        raise HTTPException(status_code=400, detail="add_value는 1 이상이어야 합니다.")

    request_event = normalize_quest_event(request.quest_event)

    matched_user_quests = (
        db.query(models.UserQuest)
        .join(models.Quest, models.UserQuest.quest_id == models.Quest.quest_id)
        .options(joinedload(models.UserQuest.quest))
        .filter(models.UserQuest.user_id == user_id)
        .filter(models.UserQuest.assigned_date == date.today())
        .filter(func.upper(models.Quest.quest_event) == request_event)
        .filter(models.Quest.is_active == True)
        .filter(models.UserQuest.is_reward_claimed == False)
        .all()
    )

    updated_count = 0
    rewarded_count = 0
    total_reward_gold = 0
    total_reward_gem = 0

    for uq in matched_user_quests:
        if uq.quest is None:
            continue

        target_value = max(1, int(uq.quest.target_value or 1))
        current_value = int(uq.current_value or 0)

        # 이미 완료됐는데 보상만 안 받은 상태도 처리
        if uq.is_completed:
            reward_gold = int(uq.quest.reward_gold or 0)
            reward_gem = int(uq.quest.reward_gem or 0)

            grant_quest_reward_if_needed(db, uq)

            if uq.is_reward_claimed:
                rewarded_count += 1
                total_reward_gold += reward_gold
                total_reward_gem += reward_gem

            continue

        uq.current_value = min(
            current_value + request.add_value,
            target_value,
        )

        if uq.current_value >= target_value:
            uq.is_completed = True
            uq.completed_at = datetime.now()

            reward_gold = int(uq.quest.reward_gold or 0)
            reward_gem = int(uq.quest.reward_gem or 0)

            grant_quest_reward_if_needed(db, uq)

            if uq.is_reward_claimed:
                rewarded_count += 1
                total_reward_gold += reward_gold
                total_reward_gem += reward_gem

        updated_count += 1

    refresh_quest_bonus_progress(db, user_id)

    db.commit()

    currency = (
        db.query(models.UserCurrency)
        .filter(models.UserCurrency.user_id == user_id)
        .first()
    )

    all_user_quests = (
        db.query(models.UserQuest)
        .join(models.Quest, models.UserQuest.quest_id == models.Quest.quest_id)
        .options(joinedload(models.UserQuest.quest))
        .filter(models.UserQuest.user_id == user_id)
        .filter(models.UserQuest.assigned_date == date.today())
        .order_by(models.UserQuest.user_quest_id.asc())
        .all()
    )

    result = [
        serialize_user_quest(uq)
        for uq in all_user_quests
        if uq.quest is not None
    ]

    return {
        "success": True,
        "user_id": user_id,
        "quest_event": request.quest_event,
        "updated_count": updated_count,
        "rewarded_count": rewarded_count,
        "reward_gold": total_reward_gold,
        "reward_gem": total_reward_gem,
        "total_gold": int(currency.gold or 0) if currency else 0,
        "total_gem": int(currency.gem or 0) if currency else 0,
        "quests": result,
    }

@app.patch("/quests/claim/{user_quest_id}")
def claim_quest_reward_for_unity(
    user_quest_id: int,
    db: Session = Depends(get_db),
):
    user_quest = (
        db.query(models.UserQuest)
        .options(joinedload(models.UserQuest.quest))
        .filter(models.UserQuest.user_quest_id == user_quest_id)
        .first()
    )

    if user_quest is None:
        raise HTTPException(
            status_code=404,
            detail="유저 퀘스트를 찾을 수 없습니다.",
        )

    if not user_quest.is_completed:
        raise HTTPException(
            status_code=400,
            detail="완료되지 않은 퀘스트입니다.",
        )

    if user_quest.is_reward_claimed:
        raise HTTPException(
            status_code=400,
            detail="이미 보상을 수령했습니다.",
        )

    currency = get_or_404(
        db,
        models.UserCurrency,
        models.UserCurrency.user_id == user_quest.user_id,
        "재화 정보를 찾을 수 없습니다.",
    )

    reward_gold = int(user_quest.quest.reward_gold or 0)
    reward_gem = int(user_quest.quest.reward_gem or 0)

    currency.gold += reward_gold
    currency.gem += reward_gem
    currency.updated_at = datetime.now()

    user_quest.is_reward_claimed = True

    db.commit()
    db.refresh(currency)
    db.refresh(user_quest)

    return {
        "success": True,
        "message": "보상을 수령했습니다.",
        "user_id": int(user_quest.user_id),
        "user_quest_id": int(user_quest.user_quest_id),
        "reward_gold": reward_gold,
        "reward_gem": reward_gem,
        "total_gold": int(currency.gold or 0),
        "total_gem": int(currency.gem or 0),
        "is_reward_claimed": bool(user_quest.is_reward_claimed),
    }

@app.post("/quests/reward-missing/{user_id}")
def reward_missing_completed_quests(
    user_id: int,
    db: Session = Depends(get_db),
):
    get_or_404(
        db,
        models.User,
        models.User.user_id == user_id,
        "사용자를 찾을 수 없습니다.",
    )

    completed_unclaimed_quests = (
        db.query(models.UserQuest)
        .options(joinedload(models.UserQuest.quest))
        .filter(models.UserQuest.user_id == user_id)
        .filter(models.UserQuest.assigned_date == date.today())
        .filter(models.UserQuest.is_completed == True)
        .filter(models.UserQuest.is_reward_claimed == False)
        .all()
    )

    reward_count = 0
    total_reward_gold = 0
    total_reward_gem = 0

    for uq in completed_unclaimed_quests:
        if uq.quest is None:
            continue

        reward_gold = int(uq.quest.reward_gold or 0)
        reward_gem = int(uq.quest.reward_gem or 0)

        grant_quest_reward_if_needed(db, uq)

        if uq.is_reward_claimed:
            reward_count += 1
            total_reward_gold += reward_gold
            total_reward_gem += reward_gem

    db.commit()

    currency = (
        db.query(models.UserCurrency)
        .filter(models.UserCurrency.user_id == user_id)
        .first()
    )

    return {
        "success": True,
        "user_id": user_id,
        "reward_count": reward_count,
        "reward_gold": total_reward_gold,
        "reward_gem": total_reward_gem,
        "total_gold": int(currency.gold or 0) if currency else 0,
        "total_gem": int(currency.gem or 0) if currency else 0,
    }


# ======================================================
# AI Feedback
# ======================================================

QUEST_EVENT_PHONE_USE = "PHONE_USE"
QUEST_EVENT_GOLD_DUN = "GOLDDUN"
QUEST_EVENT_PLAY_TIME = "PLAYTIME"
QUEST_EVENT_STAT = "STAT"
QUEST_EVENT_BATTLE_WIN = "BATTLEWIN"
QUEST_EVENT_QUEST = "QUEST"


def normalize_quest_event(event_name: str) -> str:
    if event_name is None:
        return "NONE"

    return (
        event_name
        .strip()
        .upper()
        .replace("-", "_")
        .replace(" ", "_")
    )


def normalize_condition_to_quest_types(condition_result: str) -> list[str]:
    condition = (condition_result or "NORMAL").strip().upper()

    if condition == "BEST":
        return ["상", "중", "공통", "BEST", "GOOD", "COMMON"]

    if condition == "GOOD":
        return ["중", "공통", "GOOD", "COMMON"]

    return ["하", "공통", "NORMAL", "COMMON"]


def get_recent_average_minutes(
    db: Session,
    user_id: int,
    exclude_date: date,
) -> float:
    logs = (
        db.query(models.PhoneUsageLog)
        .filter(
            models.PhoneUsageLog.user_id == user_id,
            models.PhoneUsageLog.usage_date < exclude_date,
        )
        .order_by(models.PhoneUsageLog.usage_date.desc())
        .limit(7)
        .all()
    )

    if not logs:
        return 0.0

    return sum(log.total_screen_minutes for log in logs) / len(logs)


def get_previous_condition_quest_completed(
    db: Session,
    user_id: int,
) -> int:
    yesterday = date.today() - timedelta(days=1)

    return (
        db.query(models.UserQuest)
        .join(models.Quest, models.UserQuest.quest_id == models.Quest.quest_id)
        .filter(
            models.UserQuest.user_id == user_id,
            models.UserQuest.assigned_date == yesterday,
            models.Quest.is_condition_check == True,
            models.UserQuest.is_completed == True,
        )
        .count()
    )


def get_latest_condition_result(
    db: Session,
    user_id: int,
) -> str:
    feedback = (
        db.query(models.AIFeedbackLog)
        .filter(models.AIFeedbackLog.user_id == user_id)
        .order_by(models.AIFeedbackLog.created_at.desc())
        .first()
    )

    if feedback is None:
        return "BEST"

    return feedback.condition_result or "NORMAL"


def get_today_user_quests(
    db: Session,
    user_id: int,
):
    today = date.today()

    return (
        db.query(models.UserQuest)
        .options(joinedload(models.UserQuest.quest))
        .filter(
            models.UserQuest.user_id == user_id,
            models.UserQuest.assigned_date == today,
        )
        .order_by(models.UserQuest.user_quest_id.asc())
        .all()
    )


def serialize_user_quest(user_quest: models.UserQuest) -> dict:
    quest = user_quest.quest

    current_value = int(user_quest.current_value or 0)
    target_value = int(quest.target_value or 1)

    return {
        "user_quest_id": int(user_quest.user_quest_id),
        "quest_id": int(user_quest.quest_id),

        "quest_name": quest.quest_description or "퀘스트",
        "quest_desc": quest.quest_description or "",

        "quest_type": quest.quest_type,
        "quest_grade": quest.quest_type,
        "quest_event": quest.quest_event,

        "current_value": max(0, current_value),
        "target_value": max(1, target_value),

        "reward_gold": int(quest.reward_gold or 0),
        "reward_gem": int(quest.reward_gem or 0),

        "is_completed": bool(user_quest.is_completed),
        "is_reward_claimed": bool(user_quest.is_reward_claimed),

        "assigned_date": str(user_quest.assigned_date),
        "completed_at": user_quest.completed_at.isoformat() if user_quest.completed_at else None,
    }


def assign_today_quests_by_condition(
    db: Session,
    user_id: int,
    condition_result: str,
    limit: int = 4,
):
    """
    오늘 퀘스트가 없을 때만 생성.
    일반 퀘스트 limit개 + Quest 보너스 퀘스트 1개를 할당한다.
    """
    today = date.today()

    existing_count = (
        db.query(models.UserQuest)
        .filter(
            models.UserQuest.user_id == user_id,
            models.UserQuest.assigned_date == today,
        )
        .count()
    )

    if existing_count > 0:
        return

    allowed_types = normalize_condition_to_quest_types(condition_result)

    normal_quests = (
        db.query(models.Quest)
        .filter(
            models.Quest.is_active == True,
            models.Quest.quest_type.in_(allowed_types),
            func.upper(models.Quest.quest_event) != QUEST_EVENT_QUEST,
        )
        .order_by(
            models.Quest.quest_type.asc(),
            models.Quest.quest_id.asc(),
        )
        .limit(limit)
        .all()
    )

    bonus_quest = (
        db.query(models.Quest)
        .filter(
            models.Quest.is_active == True,
            func.upper(models.Quest.quest_event) == QUEST_EVENT_QUEST,
        )
        .order_by(models.Quest.quest_id.asc())
        .first()
    )

    quests = list(normal_quests)

    if bonus_quest is not None:
        quests.append(bonus_quest)

    if len(quests) == 0:
        raise HTTPException(
            status_code=404,
            detail=f"{condition_result} 조건에 맞는 활성 퀘스트가 없습니다.",
        )

    for quest in quests:
        db.add(
            models.UserQuest(
                user_id=user_id,
                quest_id=quest.quest_id,
                current_value=0,
                is_completed=False,
                is_reward_claimed=False,
                assigned_date=today,
                completed_at=None,
            )
        )


def grant_quest_reward_if_needed(
    db: Session,
    user_quest: models.UserQuest,
):
    if user_quest is None:
        return

    if not user_quest.is_completed:
        return

    if user_quest.is_reward_claimed:
        return

    currency = get_or_404(
        db,
        models.UserCurrency,
        models.UserCurrency.user_id == user_quest.user_id,
        "재화 정보를 찾을 수 없습니다.",
    )

    reward_gold = int(user_quest.quest.reward_gold or 0)
    reward_gem = int(user_quest.quest.reward_gem or 0)

    currency.gold += reward_gold
    currency.gem += reward_gem
    currency.updated_at = datetime.now()

    user_quest.is_reward_claimed = True


def increase_today_quest_progress(
    db: Session,
    user_id: int,
    quest_event: str,
    add_value: int = 1,
):
    today = date.today()
    event_name = normalize_quest_event(quest_event)

    user_quests = (
        db.query(models.UserQuest)
        .join(models.Quest, models.UserQuest.quest_id == models.Quest.quest_id)
        .options(joinedload(models.UserQuest.quest))
        .filter(
            models.UserQuest.user_id == user_id,
            models.UserQuest.assigned_date == today,
            models.UserQuest.is_reward_claimed == False,
            func.upper(models.Quest.quest_event) == event_name,
        )
        .all()
    )

    for user_quest in user_quests:
        if user_quest.is_completed:
            grant_quest_reward_if_needed(db, user_quest)
            continue

        target_value = max(1, int(user_quest.quest.target_value or 1))
        current_value = int(user_quest.current_value or 0)

        new_value = min(target_value, current_value + max(1, add_value))

        user_quest.current_value = new_value

        if new_value >= target_value:
            user_quest.is_completed = True
            user_quest.completed_at = datetime.now()
            grant_quest_reward_if_needed(db, user_quest)

    return user_quests


def refresh_quest_bonus_progress(
    db: Session,
    user_id: int,
):
    today = date.today()

    today_quests = (
        db.query(models.UserQuest)
        .join(models.Quest, models.UserQuest.quest_id == models.Quest.quest_id)
        .options(joinedload(models.UserQuest.quest))
        .filter(
            models.UserQuest.user_id == user_id,
            models.UserQuest.assigned_date == today,
        )
        .all()
    )

    if len(today_quests) == 0:
        return

    normal_quests = [
        uq for uq in today_quests
        if normalize_quest_event(uq.quest.quest_event) != QUEST_EVENT_QUEST
    ]

    bonus_quests = [
        uq for uq in today_quests
        if normalize_quest_event(uq.quest.quest_event) == QUEST_EVENT_QUEST
    ]

    if len(normal_quests) == 0 or len(bonus_quests) == 0:
        return

    all_normal_claimed = all(
        uq.is_completed and uq.is_reward_claimed
        for uq in normal_quests
    )

    for bonus in bonus_quests:
        if all_normal_claimed:
            bonus.current_value = max(1, int(bonus.quest.target_value or 1))
            bonus.is_completed = True

            if bonus.completed_at is None:
                bonus.completed_at = datetime.now()

            grant_quest_reward_if_needed(db, bonus)
        else:
            if not bonus.is_reward_claimed:
                bonus.current_value = 0
                bonus.is_completed = False
                bonus.completed_at = None

@app.post(
    "/users/{user_id}/ai-feedbacks/generate/",
    response_model=schemas.AIFeedbackResponse,
    status_code=status.HTTP_201_CREATED,
)
def generate_ai_feedback(
    user_id: int,
    request_data: schemas.AIFeedbackGenerateRequest,
    db: Session = Depends(get_db),
):
    get_or_404(
        db,
        models.User,
        models.User.user_id == user_id,
        "사용자를 찾을 수 없습니다.",
    )

    today_feedback = (
        db.query(models.AIFeedbackLog)
        .filter(
            models.AIFeedbackLog.user_id == user_id,
            func.date(models.AIFeedbackLog.created_at) == date.today(),
        )
        .first()
    )

    if today_feedback:
        assign_today_quests_by_condition(
            db=db,
            user_id=user_id,
            condition_result=today_feedback.condition_result,
            limit=4,
        )

        refresh_quest_bonus_progress(db, user_id)

        db.commit()
        db.refresh(today_feedback)

        return today_feedback

    usage_log = None
    total_screen_minutes = request_data.total_screen_minutes

    if request_data.usage_log_id is not None:
        usage_log = get_or_404(
            db,
            models.PhoneUsageLog,
            models.PhoneUsageLog.usage_log_id == request_data.usage_log_id,
            "사용시간 기록을 찾을 수 없습니다.",
        )

        if usage_log.user_id != user_id:
            raise HTTPException(
                status_code=400,
                detail="해당 사용자의 사용시간 기록이 아닙니다.",
            )

        total_screen_minutes = usage_log.total_screen_minutes

    if total_screen_minutes is None:
        usage_log = (
            db.query(models.PhoneUsageLog)
            .filter(
                models.PhoneUsageLog.user_id == user_id,
                models.PhoneUsageLog.usage_date == date.today(),
            )
            .first()
        )

        if usage_log is None:
            raise HTTPException(
                status_code=404,
                detail="오늘 사용시간 기록이 없습니다.",
            )

        total_screen_minutes = usage_log.total_screen_minutes

    average_minutes = get_recent_average_minutes(
        db=db,
        user_id=user_id,
        exclude_date=date.today(),
    )

    previous_completed = get_previous_condition_quest_completed(
        db=db,
        user_id=user_id,
    )

    condition_result = decide_condition_result(
        today_minutes=total_screen_minutes,
        average_minutes=average_minutes,
        previous_condition_quest_completed=previous_completed,
    )

    gemini_result = call_gemini_for_feedback(
        total_screen_minutes=total_screen_minutes,
        average_minutes=average_minutes,
        previous_condition_quest_completed=previous_completed,
        condition_result=condition_result,
    )

    feedback = models.AIFeedbackLog(
        user_id=user_id,
        usage_log_id=usage_log.usage_log_id if usage_log else None,
        feedback_content=gemini_result["feedback_content"],
        pattern_summary=gemini_result["pattern_summary"],
        previous_condition_quest_completed=previous_completed,
        condition_result=condition_result,
    )

    db.add(feedback)

    create_user_daily_buffs(
        db=db,
        user_id=user_id,
        condition_result=condition_result,
    )

    assign_today_quests_by_condition(
        db=db,
        user_id=user_id,
        condition_result=condition_result,
        limit=4,
    )

    db.flush()

    increase_today_quest_progress(
        db=db,
        user_id=user_id,
        quest_event="Phone_use",
        add_value=1,
    )

    refresh_quest_bonus_progress(
        db=db,
        user_id=user_id,
    )

    db.commit()
    db.refresh(feedback)

    return feedback


@app.get(
    "/users/{user_id}/ai-feedbacks/",
    response_model=List[schemas.AIFeedbackResponse],
)
def get_user_ai_feedbacks(
    user_id: int,
    db: Session = Depends(get_db),
):
    return (
        db.query(models.AIFeedbackLog)
        .filter(models.AIFeedbackLog.user_id == user_id)
        .order_by(models.AIFeedbackLog.created_at.desc())
        .all()
    )


@app.get("/feedback/latest/{user_id}")
def get_latest_feedback_for_unity(
    user_id: int,
    db: Session = Depends(get_db),
):
    feedback = (
        db.query(models.AIFeedbackLog)
        .filter(models.AIFeedbackLog.user_id == user_id)
        .order_by(models.AIFeedbackLog.created_at.desc())
        .first()
    )

    if feedback is None:
        return {
            "success": False,
            "user_id": user_id,
            "pattern_summary": "INITIAL",
            "previous_condition_quest_completed": 0,
            "condition_result": "BEST",
            "feedback_content": "첫날은 기본 컨디션으로 시작합니다.",
            "created_at": None,
        }

    return {
        "success": True,
        "user_id": int(feedback.user_id),
        "pattern_summary": feedback.pattern_summary or "",
        "previous_condition_quest_completed": int(feedback.previous_condition_quest_completed or 0),
        "condition_result": feedback.condition_result or "NORMAL",
        "feedback_content": feedback.feedback_content,
        "created_at": feedback.created_at.isoformat() if feedback.created_at else None,
    }


# Quest API
# =========================================================
# Quest Fallback / Daily Quest Auto Assign
# =========================================================

DEFAULT_CONDITION_RESULT = "best"   # 최상
DEFAULT_QUEST_SCORE = "high"        # 상


def _model_columns(model):
    return set(model.__table__.columns.keys())


def _create_model_instance(model, **kwargs):
    columns = _model_columns(model)
    filtered = {k: v for k, v in kwargs.items() if k in columns}
    return model(**filtered)


def _seed_default_quests_if_empty(db):
    """
    quest 테이블이 비어 있으면 기본 퀘스트를 생성.
    기본값은 중 / 상 / 공통 퀘스트만 사용.
    """

    quest_count = db.query(models.Quest).count()

    if quest_count > 0:
        return

    default_quests = [
        {
            "quest_type": "중",
            "quest_event": "Phone_use",
            "quest_description": "어제 핸드폰 사용시간 평균 이하로 사용",
            "is_condition_check": False,
            "target_value": 0,
            "reward_gold": 0,
            "reward_gem": 250,
            "is_active": True,
        },
        {
            "quest_type": "중",
            "quest_event": "GoldRun",
            "quest_description": "골드 컨텐츠 3회 플레이",
            "is_condition_check": False,
            "target_value": 3,
            "reward_gold": 0,
            "reward_gem": 250,
            "is_active": True,
        },
        {
            "quest_type": "상",
            "quest_event": "Stat",
            "quest_description": "스탯 강화 10회 진행",
            "is_condition_check": False,
            "target_value": 10,
            "reward_gold": 0,
            "reward_gem": 250,
            "is_active": True,
        },
        {
            "quest_type": "공통",
            "quest_event": "BattleWin",
            "quest_description": "스테이지 10회 클리어",
            "is_condition_check": False,
            "target_value": 10,
            "reward_gold": 0,
            "reward_gem": 250,
            "is_active": True,
        },
        {
            "quest_type": "공통",
            "quest_event": "Quest",
            "quest_description": "일일 퀘스트 완료",
            "is_condition_check": False,
            "target_value": 0,
            "reward_gold": 0,
            "reward_gem": 250,
            "is_active": True,
        },
    ]

    for quest_data in default_quests:
        quest = _create_model_instance(models.Quest, **quest_data)
        db.add(quest)

    db.commit()


def _get_latest_ai_condition_or_default(user_id: int, db):
    """
    AI 피드백 로그가 있으면 최신 condition_result 사용.
    없으면 best로 고정.
    """
    if not hasattr(models, "AIFeedbackLog"):
        return DEFAULT_CONDITION_RESULT

    latest_feedback = (
        db.query(models.AIFeedbackLog)
        .filter(models.AIFeedbackLog.user_id == user_id)
        .order_by(models.AIFeedbackLog.created_at.desc())
        .first()
    )

    if latest_feedback is None:
        return DEFAULT_CONDITION_RESULT

    condition = getattr(latest_feedback, "condition_result", None)

    if condition is None or condition == "":
        return DEFAULT_CONDITION_RESULT

    return condition


def _assign_today_quests_if_empty(user_id: int, db):
    """
    user_quest에 해당 유저 퀘스트가 없으면
    quest_type이 중 / 상 / 공통인 active 퀘스트만 자동 할당.
    """

    existing_count = (
        db.query(models.UserQuest)
        .filter(models.UserQuest.user_id == user_id)
        .count()
    )

    if existing_count > 0:
        return

    _seed_default_quests_if_empty(db)

    quests = (
        db.query(models.Quest)
        .filter(models.Quest.is_active == True)
        .filter(models.Quest.quest_type.in_(["중", "상", "공통"]))
        .all()
    )

    for quest in quests:
        user_quest = _create_model_instance(
            models.UserQuest,
            user_id=user_id,
            quest_id=quest.quest_id,
            current_value=0,
            is_completed=False,
            is_reward_claimed=False,
            assigned_date=date.today(),
        )
        db.add(user_quest)

    db.commit()


@app.get("/users/{user_id}/quests/popup")
def get_user_quest_popup(user_id: int, db: Session = Depends(get_db)):
    """
    QuestPopupController에서 호출할 API.

    역할:
    1. 유저 존재 확인
    2. AI 결과가 없으면 condition_result = best
    3. 퀘스트 스코어가 없으면 quest_score = high
    4. user_quest가 비어 있으면 자동 생성
    5. 팝업에 띄울 퀘스트 목록 반환
    """

    user = db.query(models.User).filter(models.User.user_id == user_id).first()

    if user is None:
        raise HTTPException(status_code=404, detail="유저를 찾을 수 없습니다.")

    condition_result = _get_latest_ai_condition_or_default(user_id, db)
    quest_score = DEFAULT_QUEST_SCORE

    _assign_today_quests_if_empty(user_id, db)

    user_quests = (
        db.query(models.UserQuest)
        .filter(models.UserQuest.user_id == user_id)
        .all()
    )

    result = []

    for uq in user_quests:
        quest = db.query(models.Quest).filter(models.Quest.quest_id == uq.quest_id).first()

        if quest is None:
            continue

        reward_gold = getattr(quest, "reward_gold", 0)
        reward_gem = getattr(quest, "reward_gem", 0)

        progress = getattr(uq, "progress", 0)
        target_progress = getattr(uq, "target_progress", 1)

        result.append(
            {
                "user_quest_id": uq.user_quest_id,
                "quest_id": quest.quest_id,

                "quest_name": getattr(quest, "quest_description", ""),
                "quest_desc": getattr(quest, "quest_description", ""),

                "quest_type": getattr(quest, "quest_type", ""),
                "quest_grade": getattr(quest, "quest_grade", ""),
                "quest_event": getattr(quest, "quest_event", ""),

                "current_value": getattr(uq, "current_value", 0),
                "target_value": getattr(quest, "target_value", 1),

                "reward_gold": getattr(quest, "reward_gold", 0),
                "reward_gem": getattr(quest, "reward_gem", 0),

                "is_completed": uq.is_completed,
                "is_reward_claimed": uq.is_reward_claimed,

                "assigned_date": str(getattr(uq, "assigned_date", "")),
                "completed_at": str(getattr(uq, "completed_at", "")),
            }
        )

    return {
        "success": True,
        "user_id": user_id,
        "condition_result": condition_result,
        "condition_text": "최상",
        "quest_score": quest_score,
        "quest_score_text": "상",
        "quests": result,
    }








# ======================================================
# User Buff
# ======================================================

@app.get("/users/{user_id}/buffs/")
def get_user_buffs(user_id: int, db: Session = Depends(get_db)):
    rows = db.execute(
        text(
            """
            SELECT
                ub.user_id,
                ub.buff_id,
                ub.buff_type,
                ub.condition_score,
                ub.current_effect_value,
                ub.buff_date,
                ub.is_active,
                ub.applied_at,
                ub.updated_at,
                bi.buff_name,
                bi.condition_grade,
                bi.is_decaying,
                bi.decay_value
            FROM user_buff ub
            JOIN buff_info bi
                ON ub.buff_id = bi.buff_id
            WHERE ub.user_id = :user_id
            ORDER BY ub.buff_date DESC, ub.buff_type
            """
        ),
        {"user_id": user_id},
    ).mappings().all()

    return [dict(row) for row in rows]


@app.post("/users/{user_id}/buffs/create-today/")
def create_today_user_buffs(user_id: int, db: Session = Depends(get_db)):
    get_or_404(
        db,
        models.User,
        models.User.user_id == user_id,
        "사용자를 찾을 수 없습니다.",
    )

    create_user_daily_buffs(
        db=db,
        user_id=user_id,
        condition_result="BEST",
    )

    db.commit()

    return {
        "success": True,
        "message": "오늘 기본 버프 4개를 생성했습니다.",
        "user_id": user_id,
    }


# ======================================================
# Test / Reset
# ======================================================

@app.delete("/test/user-items/reset")
def reset_user_items(db: Session = Depends(get_db)):
    deleted_count = db.query(models.UserItem).delete()
    db.commit()

    return {
        "success": True,
        "message": "user_item 목록이 초기화되었습니다.",
        "deleted_count": deleted_count,
    }


# ==========================================
# 1. [GET] 유저의 최근 7일 폰 사용량 및 어제 퀘스트 완료 수 조회 API
# ==========================================
@app.get("/usage-logs/recent/{user_id}", response_model=schemas.RecentUsageResponse)
def get_recent_usage(user_id: int, db: Session = Depends(get_db)):
    # 1. 최근 7일 폰 사용량 가져오기 (가장 최근 날짜부터 7개)
    logs = db.query(models.PhoneUsageLog).filter(models.PhoneUsageLog.user_id == user_id) \
        .order_by(desc(models.PhoneUsageLog.usage_date)).limit(7).all()

    # DB에 저장된 분(minutes) 데이터 추출
    minutes_list = [log.total_screen_minutes for log in logs]

    # 만약 유저가 가입한 지 얼마 안 돼서 데이터가 7개 미만이면, 기본값(예: 240분)으로 채움
    while len(minutes_list) < 7:
        minutes_list.append(240)
    minutes_list.reverse()  # 과거 -> 어제 순서로 뒤집기

    # 어제 사용량 (가장 마지막 값)
    yesterday_minutes = minutes_list[-1]

    # 2. 어제 완료한 퀘스트 개수 가져오기
    yesterday = date.today() - timedelta(days=1)
    completed_quests = db.query(models.UserQuest).filter(
        models.UserQuest.user_id == user_id,
        models.UserQuest.assigned_date == yesterday,
        models.UserQuest.is_completed == True
    ).count()

    return {
        "recent_7days_minutes": minutes_list,
        "yesterday_minutes": yesterday_minutes,
        "yesterday_quest_completed": completed_quests
    }


# ==========================================
# 2. [POST] AI 분석 결과(피드백 및 퀘스트) DB 일괄 저장 API
# ==========================================
# ==========================================
# 2. [POST] AI 분석 결과(피드백 및 퀘스트) DB 일괄 저장 API
# ==========================================
@app.post("/ai-feedbacks/")
def create_ai_feedback(feedback_data: schemas.AIFeedbackCreate, db: Session = Depends(get_db)):
    # 1. ai_feedback_log 테이블에 멘트와 등급 저장
    new_feedback = models.AIFeedbackLog(
        user_id=feedback_data.user_id,
        feedback_content=feedback_data.feedback_content,
        pattern_summary=feedback_data.pattern_summary,
        condition_result=feedback_data.condition_result
    )
    db.add(new_feedback)

    # 2. user_quest 재할당
    #    재분석 시 이전 결과 퀘스트가 남아 새 결과와 섞이는 문제 방지.
    #    "오늘 날짜 + 해당 유저"의 기존 퀘스트를 먼저 비우고 새로 넣는다.
    today = date.today()

    # 2-1. 오늘 발급분 삭제 (과거 날짜 기록은 보존)
    #      완료/보상받은 오늘 퀘스트도 함께 지워지는 점에 유의.
    #      (재분석은 같은 날 테스트 상황 가정. 완료 기록 보존이 필요하면 아래 주석의 조건 추가)
    db.query(models.UserQuest).filter(
        models.UserQuest.user_id == feedback_data.user_id,
        models.UserQuest.assigned_date == today
        # 완료/보상받은 퀘스트는 남기려면 아래 줄의 주석을 해제:
        # , models.UserQuest.is_completed == False
        # , models.UserQuest.is_reward_claimed == False
    ).delete(synchronize_session=False)

    # 2-2. 이번 분석 결과로 새로 할당
    for q_id in feedback_data.assigned_quest_ids:
        new_user_quest = models.UserQuest(
            user_id=feedback_data.user_id,
            quest_id=q_id,
            current_value=0,
            is_completed=False,
            is_reward_claimed=False,
            assigned_date=today
        )
        db.add(new_user_quest)

    # 3. 변경사항 확정 (DB Commit)
    db.commit()

    return {"status": "success", "message": "AI 피드백 로깅 및 퀘스트 재할당이 완료되었습니다."}