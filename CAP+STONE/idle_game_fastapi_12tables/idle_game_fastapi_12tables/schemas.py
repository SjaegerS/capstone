from datetime import date, datetime
from typing import Optional

from pydantic import BaseModel, EmailStr, Field


class CharacterInfoCreate(BaseModel):
    character_key: str
    character_name: str
    description: Optional[str] = None
    main_effect: Optional[str] = None


class CharacterInfoUpdate(BaseModel):
    character_key: Optional[str] = None
    character_name: Optional[str] = None
    description: Optional[str] = None
    main_effect: Optional[str] = None


class CharacterInfoResponse(BaseModel):
    character_id: int
    character_key: str
    character_name: str
    description: Optional[str] = None
    main_effect: Optional[str] = None

    class Config:
        from_attributes = True


class UserCreate(BaseModel):
    email: EmailStr
    password_hash: str
    nickname: str
    default_character_id: int


class UserUpdate(BaseModel):
    email: Optional[EmailStr] = None
    password_hash: Optional[str] = None
    nickname: Optional[str] = None
    last_login_at: Optional[datetime] = None


class UserStatusResponse(BaseModel):
    user_id: int
    current_character_id: int
    player_level: int
    player_exp: int
    required_exp: int
    current_stage: int
    total_boss_kill_count: int

    max_hp: int
    attack_power: int
    defense_power: int

    hp_upgrade_lvl: int
    attack_upgrade_lvl: int
    defense_upgrade_lvl: int


    updated_at: datetime
    current_character: Optional[CharacterInfoResponse] = None

    class Config:
        from_attributes = True

class UserStatUpgradeResponse(BaseModel):
    user_id: int
    upgrade_type: str

    max_hp: int
    attack_power: int
    defense_power: int

    hp_upgrade_lvl: int
    attack_upgrade_lvl: int
    defense_upgrade_lvl: int

    gold: int
    cost_gold: int

    class Config:
        from_attributes = True

class UserResponse(BaseModel):
    user_id: int
    email: str
    nickname: str
    created_at: datetime
    last_login_at: Optional[datetime] = None
    status: Optional[UserStatusResponse] = None

    class Config:
        from_attributes = True


class CharacterStatusResponse(BaseModel):
    user_id: int
    character_id: int
    character_level: int
    character: Optional[CharacterInfoResponse] = None

    class Config:
        from_attributes = True


class CharacterStatusUpdate(BaseModel):
    character_level: Optional[int] = None


class CharacterConditionResponse(BaseModel):
    user_id: int
    character_id: int
    condition_score: int
    condition_grade: str
    last_updated_date: date
    character: Optional[CharacterInfoResponse] = None

    class Config:
        from_attributes = True


class CharacterConditionUpdate(BaseModel):
    condition_score: Optional[int] = Field(default=None, ge=1, le=3)
    condition_grade: Optional[str] = None
    last_updated_date: Optional[date] = None


class ItemCreate(BaseModel):
    item_key: str
    item_name: str
    item_type: str
    grade: str = "NORMAL"
    image_key: str
    base_attack: int = 0
    base_defense: int = 0


class ItemUpdate(BaseModel):
    item_key: Optional[str] = None
    item_name: Optional[str] = None
    item_type: Optional[str] = None
    grade: Optional[str] = None
    image_key: Optional[str] = None
    base_attack: Optional[int] = None
    base_defense: Optional[int] = None


class ItemResponse(BaseModel):
    item_id: int
    item_key: str
    item_name: str
    item_type: str
    grade: str
    image_key: str
    base_attack: int
    base_defense: int

    class Config:
        from_attributes = True


class UserItemCreate(BaseModel):
    user_id: int
    item_id: int
    quantity: int = 1
    enhance_level: int = 0
    is_equipped: bool = False


class UserItemUpdate(BaseModel):
    enhance_level: Optional[int] = Field(default=None, ge=0, le=5)
    is_equipped: Optional[bool] = None


class UserItemResponse(BaseModel):
    user_item_id: int
    user_id: int
    item_id: int
    quantity: int
    enhance_level: int
    is_equipped: bool
    item: Optional[ItemResponse] = None

    class Config:
        from_attributes = True


class UserCurrencyResponse(BaseModel):
    user_id: int
    gold: int
    gem: int
    updated_at: datetime

    class Config:
        from_attributes = True

class SpendCurrencyRequest(BaseModel):
    amount: int

class UserCurrencyUpdate(BaseModel):
    gold: Optional[int] = None
    gem: Optional[int] = None


class BattleRewardRequest(BaseModel):
    user_id: int
    stage_id: int
    reward_gold: int
    reward_exp: int
    kill_count_add: int = 1

class PhoneUsageLogCreate(BaseModel):
    user_id: int
    usage_date: date
    total_screen_minutes: int = 0


class PhoneUsageLogResponse(BaseModel):
    usage_log_id: int
    user_id: int
    usage_date: date
    total_screen_minutes: int
    created_at: datetime

    class Config:
        from_attributes = True


class QuestCreate(BaseModel):
    quest_name: str
    quest_description: Optional[str] = None
    is_condition_check: bool = False
    target_value: int
    reward_gold: int = 0
    reward_gem: int = 0
    is_active: bool = True


class QuestUpdate(BaseModel):
    quest_name: Optional[str] = None
    quest_description: Optional[str] = None
    is_condition_check: Optional[bool] = None
    target_value: Optional[int] = None
    reward_gold: Optional[int] = None
    reward_gem: Optional[int] = None
    is_active: Optional[bool] = None


class QuestResponse(BaseModel):
    quest_id: int
    quest_name: str
    quest_description: Optional[str] = None
    is_condition_check: bool
    target_value: int
    reward_gold: int
    reward_gem: int
    is_active: bool

    class Config:
        from_attributes = True


class UserQuestCreate(BaseModel):
    user_id: int
    quest_id: int
    progress_value: int = 0
    is_completed: bool = False
    is_reward_claimed: bool = False
    assigned_date: date
    completed_at: Optional[datetime] = None


class UserQuestUpdate(BaseModel):
    progress_value: Optional[int] = None
    is_completed: Optional[bool] = None
    is_reward_claimed: Optional[bool] = None
    completed_at: Optional[datetime] = None


class UserQuestResponse(BaseModel):
    user_quest_id: int
    user_id: int
    quest_id: int
    progress_value: int
    is_completed: bool
    is_reward_claimed: bool
    assigned_date: date
    completed_at: Optional[datetime] = None
    quest: Optional[QuestResponse] = None

    class Config:
        from_attributes = True


class OfflineRewardBoxResponse(BaseModel):
    reward_box_id: int
    user_id: int
    accumulated_min: int
    boss_kill_count: int
    reward_gold: int
    reward_exp: int
    is_claimed: bool
    created_at: datetime
    claimed_at: Optional[datetime] = None

    class Config:
        from_attributes = True


class AIFeedbackGenerateRequest(BaseModel):
    usage_log_id: Optional[int] = None
    total_screen_minutes: Optional[int] = None


class AIFeedbackResponse(BaseModel):
    feedback_id: int
    user_id: int
    usage_log_id: Optional[int] = None
    feedback_content: str
    pattern_summary: Optional[str] = None
    previous_condition_quest_completed: int
    condition_result: str
    created_at: datetime

    class Config:
        from_attributes = True