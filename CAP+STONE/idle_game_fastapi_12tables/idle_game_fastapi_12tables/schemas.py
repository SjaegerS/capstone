from pydantic import BaseModel, EmailStr
from datetime import date, datetime
from typing import Optional, List


# --------------------
# Character Type
# --------------------

class CharacterTypeResponse(BaseModel):
    character_type_id: int
    type_code: str
    type_name: str
    description: Optional[str] = None
    main_effect: Optional[str] = None

    class Config:
        from_attributes = True


# --------------------
# User
# --------------------

class UserCreate(BaseModel):
    email: EmailStr
    password_hash: str
    nickname: str
    default_character_type_id: int = 1


class UserUpdate(BaseModel):
    email: Optional[EmailStr] = None
    password_hash: Optional[str] = None
    nickname: Optional[str] = None
    last_login_at: Optional[datetime] = None


class UserResponse(BaseModel):
    user_id: int
    email: str
    nickname: str
    created_at: datetime
    last_login_at: Optional[datetime] = None

    class Config:
        from_attributes = True


# --------------------
# Character Status
# --------------------

class CharacterStatusUpdate(BaseModel):
    character_type_id: Optional[int] = None
    level: Optional[int] = None
    exp: Optional[int] = None
    required_exp: Optional[int] = None
    attack_power: Optional[int] = None
    defense_power: Optional[int] = None
    current_stage: Optional[int] = None
    total_boss_kill_count: Optional[int] = None


class CharacterStatusResponse(BaseModel):
    character_id: int
    user_id: int
    character_type_id: int
    level: int
    exp: int
    required_exp: int
    attack_power: int
    defense_power: int
    current_stage: int
    total_boss_kill_count: int
    updated_at: datetime
    character_type: Optional[CharacterTypeResponse] = None

    class Config:
        from_attributes = True


# --------------------
# Character Condition
# --------------------

class CharacterConditionUpdate(BaseModel):
    condition_score: Optional[int] = None
    condition_grade: Optional[str] = None
    last_updated_date: Optional[date] = None


class CharacterConditionResponse(BaseModel):
    condition_id: int
    user_id: int
    character_type_id: int
    condition_score: int
    condition_grade: str
    last_updated_date: date
    character_type: Optional[CharacterTypeResponse] = None

    class Config:
        from_attributes = True


# --------------------
# Currency
# --------------------

class UserCurrencyUpdate(BaseModel):
    gold: Optional[int] = None
    gem: Optional[int] = None


class UserCurrencyResponse(BaseModel):
    currency_id: int
    user_id: int
    gold: int
    gem: int
    updated_at: datetime

    class Config:
        from_attributes = True


# --------------------
# Item
# --------------------

class ItemCreate(BaseModel):
    item_name: str
    item_type: str
    grade: str = "NORMAL"
    base_attack: int = 0
    base_defense: int = 0
    base_effect: Optional[str] = None
    sell_price: int = 0


class ItemUpdate(BaseModel):
    item_name: Optional[str] = None
    item_type: Optional[str] = None
    grade: Optional[str] = None
    base_attack: Optional[int] = None
    base_defense: Optional[int] = None
    base_effect: Optional[str] = None
    sell_price: Optional[int] = None


class ItemResponse(ItemCreate):
    item_id: int

    class Config:
        from_attributes = True


# --------------------
# User Item
# --------------------

class UserItemCreate(BaseModel):
    user_id: int
    item_id: int
    enhance_level: int = 0
    is_equipped: bool = False


class UserItemUpdate(BaseModel):
    enhance_level: Optional[int] = None
    is_equipped: Optional[bool] = None


class UserItemResponse(BaseModel):
    user_item_id: int
    user_id: int
    item_id: int
    enhance_level: int
    is_equipped: bool
    obtained_at: datetime
    item: Optional[ItemResponse] = None

    class Config:
        from_attributes = True


# --------------------
# Quest
# --------------------

class QuestCreate(BaseModel):
    quest_name: str
    quest_description: Optional[str] = None
    quest_type: str
    target_value: int
    reward_gold: int = 0
    reward_gem: int = 0
    reward_exp: int = 0
    condition_recovery: int = 0
    is_active: bool = True


class QuestUpdate(BaseModel):
    quest_name: Optional[str] = None
    quest_description: Optional[str] = None
    quest_type: Optional[str] = None
    target_value: Optional[int] = None
    reward_gold: Optional[int] = None
    reward_gem: Optional[int] = None
    reward_exp: Optional[int] = None
    condition_recovery: Optional[int] = None
    is_active: Optional[bool] = None


class QuestResponse(QuestCreate):
    quest_id: int

    class Config:
        from_attributes = True


# --------------------
# User Quest
# --------------------

class UserQuestCreate(BaseModel):
    user_id: int
    quest_id: int
    progress_value: int = 0
    is_accepted: bool = True
    is_completed: bool = False
    is_reward_claimed: bool = False
    assigned_date: Optional[date] = None


class UserQuestUpdate(BaseModel):
    progress_value: Optional[int] = None
    is_accepted: Optional[bool] = None
    is_completed: Optional[bool] = None
    is_reward_claimed: Optional[bool] = None
    completed_at: Optional[datetime] = None


class UserQuestResponse(BaseModel):
    user_quest_id: int
    user_id: int
    quest_id: int
    progress_value: int
    is_accepted: bool
    is_completed: bool
    is_reward_claimed: bool
    assigned_date: date
    completed_at: Optional[datetime] = None
    quest: Optional[QuestResponse] = None

    class Config:
        from_attributes = True


# --------------------
# Phone Usage Log
# --------------------

class PhoneUsageLogCreate(BaseModel):
    user_id: int
    usage_date: date
    total_screen_minutes: int = 0
    target_app_minutes: int = 0
    night_usage_minutes: int = 0
    is_life_pattern_good: bool = True


class PhoneUsageLogResponse(PhoneUsageLogCreate):
    usage_log_id: int
    created_at: datetime

    class Config:
        from_attributes = True


# --------------------
# AI Feedback Log
# --------------------

class AIFeedbackCreate(BaseModel):
    user_id: int
    usage_log_id: Optional[int] = None
    feedback_content: str
    pattern_summary: Optional[str] = None
    quest_suggestion: Optional[str] = None
    condition_result: str = "NORMAL"


class AIFeedbackResponse(AIFeedbackCreate):
    feedback_id: int
    created_at: datetime

    class Config:
        from_attributes = True


# --------------------
# Offline Reward Box
# --------------------

class OfflineRewardBoxCreate(BaseModel):
    user_id: int
    accumulated_seconds: int = 0
    boss_kill_count: int = 0
    reward_gold: int = 0
    reward_exp: int = 0
    is_claimed: bool = False


class OfflineRewardBoxUpdate(BaseModel):
    accumulated_seconds: Optional[int] = None
    boss_kill_count: Optional[int] = None
    reward_gold: Optional[int] = None
    reward_exp: Optional[int] = None
    is_claimed: Optional[bool] = None
    claimed_at: Optional[datetime] = None


class OfflineRewardBoxResponse(OfflineRewardBoxCreate):
    reward_box_id: int
    created_at: datetime
    claimed_at: Optional[datetime] = None

    class Config:
        from_attributes = True


# --------------------
# Joined response
# --------------------

class UserDetailResponse(UserResponse):
    character_status: Optional[CharacterStatusResponse] = None
    currency: Optional[UserCurrencyResponse] = None
    conditions: List[CharacterConditionResponse] = []
    user_items: List[UserItemResponse] = []
    user_quests: List[UserQuestResponse] = []
    usage_logs: List[PhoneUsageLogResponse] = []
    ai_feedbacks: List[AIFeedbackResponse] = []
    offline_reward_boxes: List[OfflineRewardBoxResponse] = []

    class Config:
        from_attributes = True
