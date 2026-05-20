from datetime import date, datetime
from typing import List, Optional

from pydantic import BaseModel, EmailStr, Field


class CharacterInfoCreate(BaseModel):
    character_key: str
    character_name: str
    description: Optional[str] = None
    main_effect: Optional[str] = None
    image_key: str


class CharacterInfoUpdate(BaseModel):
    character_key: Optional[str] = None
    character_name: Optional[str] = None
    description: Optional[str] = None
    main_effect: Optional[str] = None
    image_key: Optional[str] = None


class CharacterInfoResponse(BaseModel):
    character_id: int
    character_key: str
    character_name: str
    description: Optional[str] = None
    main_effect: Optional[str] = None
    image_key: str

    class Config:
        from_attributes = True


class UserCreate(BaseModel):
    email: EmailStr
    password_hash: str
    nickname: str
    default_character_id: int = 1


class UserUpdate(BaseModel):
    email: Optional[EmailStr] = None
    password_hash: Optional[str] = None
    nickname: Optional[str] = None
    current_character_id: Optional[int] = None
    last_login_at: Optional[datetime] = None


class UserResponse(BaseModel):
    user_id: int
    current_character_id: int
    email: str
    nickname: str
    created_at: datetime
    last_login_at: Optional[datetime] = None
    current_character: Optional[CharacterInfoResponse] = None

    class Config:
        from_attributes = True


class CharacterStatusUpdate(BaseModel):
    level: Optional[int] = None
    exp: Optional[int] = None
    required_exp: Optional[int] = None
    max_hp: Optional[int] = None
    current_hp: Optional[int] = None
    attack_power: Optional[int] = None
    defense_power: Optional[int] = None
    current_stage: Optional[int] = None
    total_boss_kill_count: Optional[int] = None


class CharacterStatusResponse(BaseModel):
    status_id: int
    user_id: int
    character_id: int
    level: int
    exp: int
    required_exp: int
    max_hp: int
    current_hp: int
    attack_power: int
    defense_power: int
    current_stage: int
    total_boss_kill_count: int
    updated_at: datetime
    character: Optional[CharacterInfoResponse] = None

    class Config:
        from_attributes = True


class CharacterConditionUpdate(BaseModel):
    condition_score: Optional[int] = Field(default=None, ge=1, le=3)
    condition_grade: Optional[str] = None
    last_updated_date: Optional[date] = None


class CharacterConditionResponse(BaseModel):
    condition_id: int
    user_id: int
    character_id: int
    condition_score: int
    condition_grade: str
    last_updated_date: date
    character: Optional[CharacterInfoResponse] = None

    class Config:
        from_attributes = True


class UserCurrencyUpdate(BaseModel):
    gold: Optional[int] = None


class UserCurrencyResponse(BaseModel):
    currency_id: int
    user_id: int
    gold: int
    updated_at: datetime

    class Config:
        from_attributes = True


class ItemCreate(BaseModel):
    item_key: str
    item_name: str
    item_type: str
    grade: str = "NORMAL"
    image_key: str
    base_attack: int = 0
    base_defense: int = 0
    base_effect: Optional[str] = None
    enhance_base_cost: int = 100


class ItemUpdate(BaseModel):
    item_key: Optional[str] = None
    item_name: Optional[str] = None
    item_type: Optional[str] = None
    grade: Optional[str] = None
    image_key: Optional[str] = None
    base_attack: Optional[int] = None
    base_defense: Optional[int] = None
    base_effect: Optional[str] = None
    enhance_base_cost: Optional[int] = None


class ItemResponse(BaseModel):
    item_id: int
    item_key: str
    item_name: str
    item_type: str
    grade: str
    image_key: str
    base_attack: int
    base_defense: int
    base_effect: Optional[str] = None
    enhance_base_cost: int

    class Config:
        from_attributes = True


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


class QuestCreate(BaseModel):
    quest_key: str
    quest_name: str
    quest_description: Optional[str] = None
    quest_type: str
    image_key: str
    target_value: int
    reward_gold: int = 0
    reward_exp: int = 0
    condition_recovery: int = 0
    is_active: bool = True


class QuestUpdate(BaseModel):
    quest_key: Optional[str] = None
    quest_name: Optional[str] = None
    quest_description: Optional[str] = None
    quest_type: Optional[str] = None
    image_key: Optional[str] = None
    target_value: Optional[int] = None
    reward_gold: Optional[int] = None
    reward_exp: Optional[int] = None
    condition_recovery: Optional[int] = None
    is_active: Optional[bool] = None


class QuestResponse(BaseModel):
    quest_id: int
    quest_key: str
    quest_name: str
    quest_description: Optional[str] = None
    quest_type: str
    image_key: str
    target_value: int
    reward_gold: int
    reward_exp: int
    condition_recovery: int
    is_active: bool

    class Config:
        from_attributes = True


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


class PhoneUsageLogCreate(BaseModel):
    user_id: int
    usage_date: date
    total_screen_minutes: int = 0


class PhoneUsageLogUpdate(BaseModel):
    total_screen_minutes: Optional[int] = None


class PhoneUsageLogResponse(BaseModel):
    usage_log_id: int
    user_id: int
    usage_date: date
    total_screen_minutes: int
    created_at: datetime

    class Config:
        from_attributes = True


class AIFeedbackCreate(BaseModel):
    user_id: int
    usage_log_id: Optional[int] = None
    feedback_content: str
    pattern_summary: Optional[str] = None
    quest_suggestion: Optional[str] = None
    condition_result: str = "NORMAL"


class AIFeedbackResponse(BaseModel):
    feedback_id: int
    user_id: int
    usage_log_id: Optional[int] = None
    feedback_content: str
    pattern_summary: Optional[str] = None
    quest_suggestion: Optional[str] = None
    condition_result: str
    created_at: datetime

    class Config:
        from_attributes = True


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


class OfflineRewardBoxResponse(BaseModel):
    reward_box_id: int
    user_id: int
    accumulated_seconds: int
    boss_kill_count: int
    reward_gold: int
    reward_exp: int
    is_claimed: bool
    created_at: datetime
    claimed_at: Optional[datetime] = None

    class Config:
        from_attributes = True


class UserDetailResponse(UserResponse):
    character_statuses: List[CharacterStatusResponse] = []
    currency: Optional[UserCurrencyResponse] = None
    conditions: List[CharacterConditionResponse] = []
    user_items: List[UserItemResponse] = []
    user_quests: List[UserQuestResponse] = []
    usage_logs: List[PhoneUsageLogResponse] = []
    ai_feedbacks: List[AIFeedbackResponse] = []
    offline_reward_boxes: List[OfflineRewardBoxResponse] = []

    class Config:
        from_attributes = True