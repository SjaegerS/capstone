from datetime import date, datetime
from typing import List, Optional

from pydantic import BaseModel, EmailStr, Field


class CharacterInfoCreate(BaseModel):
    character_key: str
    character_name: str


class CharacterInfoUpdate(BaseModel):
    character_key: Optional[str] = None
    character_name: Optional[str] = None


class CharacterInfoResponse(BaseModel):
    character_id: int
    character_key: str
    character_name: str

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
    current_character_id: Optional[int] = None

    player_level: int = 1
    player_exp: int = 0
    required_exp: int = 1000

    hp_upgrade_lvl: int = 1
    attack_upgrade_lvl: int = 1
    defense_upgrade_lvl: int = 1

    max_hp: int = 100
    attack_power: int = 20
    defense_power: int = 20

    current_stage: int = 1
    max_cleared_stage: int = 0
    total_boss_kill_count: int = 0
    updated_at: Optional[datetime] = None

    current_character: Optional[CharacterInfoResponse] = None

    class Config:
        from_attributes = True


class UserLevelStatusResponse(BaseModel):
    user_id: int
    level: int
    exp: int
    required_exp: int
    gem: int


class UserStatUpgradeResponse(BaseModel):
    user_id: int
    upgrade_type: str

    max_hp: int
    attack_power: int
    defense_power: int

    hp_upgrade_lvl: int
    attack_upgrade_lvl: int
    defense_upgrade_lvl: int

    upgrade_lvl: Optional[int] = None

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
    character: Optional[CharacterInfoResponse] = None

    class Config:
        from_attributes = True


class CharacterStatusUpdate(BaseModel):
    pass


class BuffInfoCreate(BaseModel):
    buff_type: str
    condition_grade: str
    buff_name: str
    effect_value: float = 0
    is_decaying: bool = True
    decay_value: float = 0


class BuffInfoUpdate(BaseModel):
    buff_type: Optional[str] = None
    condition_grade: Optional[str] = None
    buff_name: Optional[str] = None
    effect_value: Optional[float] = None
    is_decaying: Optional[bool] = None
    decay_value: Optional[float] = None


class BuffInfoResponse(BaseModel):
    buff_id: int
    buff_type: str
    condition_grade: str
    buff_name: str
    effect_value: float
    is_decaying: bool
    decay_value: float

    class Config:
        from_attributes = True


class UserBuffCreate(BaseModel):
    user_id: int
    buff_id: int
    buff_type: str
    condition_score: int = Field(ge=0, le=100)
    current_effect_value: float = Field(ge=0)
    buff_date: date
    is_active: bool = True


class UserBuffUpdate(BaseModel):
    buff_id: Optional[int] = None
    condition_score: Optional[int] = Field(default=None, ge=0, le=100)
    current_effect_value: Optional[float] = Field(default=None, ge=0)
    is_active: Optional[bool] = None


class UserBuffResponse(BaseModel):
    user_id: int
    buff_id: int
    buff_type: str
    condition_score: int
    current_effect_value: float
    buff_date: date
    is_active: bool
    applied_at: datetime
    updated_at: datetime
    buff_info: Optional[BuffInfoResponse] = None

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
    quantity: int = Field(default=1, ge=1)
    enhance_level: int = Field(default=1, ge=1)
    is_equipped: bool = False


class UserItemUpdate(BaseModel):
    enhance_level: Optional[int] = Field(default=None, ge=1)
    is_equipped: Optional[bool] = None
    quantity: Optional[int] = Field(default=None, ge=0)


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
    amount: int = Field(gt=0)


class UserCurrencyUpdate(BaseModel):
    gold: Optional[int] = Field(default=None, ge=0)
    gem: Optional[int] = Field(default=None, ge=0)


class BattleRewardRequest(BaseModel):
    user_id: int
    stage_id: int = Field(ge=1)
    is_clear: bool
    kill_count_add: int = Field(default=1, ge=0)
    reward_gold: int = Field(default=0, ge=0)
    reward_exp: int = Field(default=0, ge=0)


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
    quest_type: str = "공통"
    quest_event: str = "NONE"
    quest_description: Optional[str] = None
    is_condition_check: bool = False
    target_value: int
    reward_gold: int = 0
    reward_gem: int = 0
    is_active: bool = True


class QuestUpdate(BaseModel):
    quest_type: Optional[str] = None
    quest_event: Optional[str] = None
    quest_description: Optional[str] = None
    is_condition_check: Optional[bool] = None
    target_value: Optional[int] = None
    reward_gold: Optional[int] = None
    reward_gem: Optional[int] = None
    is_active: Optional[bool] = None


class QuestResponse(BaseModel):
    quest_id: int
    quest_type: str
    quest_event: str
    quest_description: Optional[str] = None
    is_condition_check: bool
    target_value: int
    reward_gold: int
    reward_gem: int
    is_active: bool

    class Config:
        from_attributes = True


class QuestProgressRequest(BaseModel):
    quest_event: str
    add_value: int


class UserQuestCreate(BaseModel):
    user_id: int
    quest_id: int
    current_value: int = 0
    is_completed: bool = False
    is_reward_claimed: bool = False
    assigned_date: date
    completed_at: Optional[datetime] = None


class UserQuestUpdate(BaseModel):
    current_value: Optional[int] = None
    is_completed: Optional[bool] = None
    is_reward_claimed: Optional[bool] = None
    completed_at: Optional[datetime] = None


class UserQuestResponse(BaseModel):
    user_quest_id: int
    user_id: int
    quest_id: int
    current_value: int
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
    reward_gem: int
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


class AIFeedbackCreate(BaseModel):
    user_id: int
    pattern_summary: str
    usage_score: float
    condition_result: str
    feedback_content: str
    assigned_quest_ids: List[int]


class RecentUsageResponse(BaseModel):
    recent_7days_minutes: List[int]
    yesterday_minutes: int
    yesterday_quest_completed: int
