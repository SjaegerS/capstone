from sqlalchemy import (
    BigInteger,
    Boolean,
    CheckConstraint,
    Column,
    Date,
    DateTime,
    ForeignKey,
    Integer,
    Numeric,
    String,
    Text,
    UniqueConstraint,
    func,
)
from sqlalchemy.orm import relationship
from sqlalchemy import Column, Integer, DateTime, ForeignKey

from database import Base


class CharacterInfo(Base):
    __tablename__ = "character_info"

    character_id = Column(BigInteger, primary_key=True, autoincrement=True)
    character_key = Column(String(100), nullable=False, unique=True)
    character_name = Column(String(50), nullable=False)

    character_statuses = relationship(
        "CharacterStatus",
        back_populates="character",
        cascade="all, delete-orphan",
    )


class User(Base):
    __tablename__ = "users"

    user_id = Column(BigInteger, primary_key=True, autoincrement=True)
    email = Column(String(100), nullable=False, unique=True)
    password_hash = Column(String(255), nullable=False)
    nickname = Column(String(50), nullable=False)
    created_at = Column(DateTime, server_default=func.now())
    last_login_at = Column(DateTime, nullable=True)

    status = relationship(
        "UserStatus",
        back_populates="user",
        uselist=False,
        cascade="all, delete-orphan",
    )

    currency = relationship(
        "UserCurrency",
        back_populates="user",
        uselist=False,
        cascade="all, delete-orphan",
    )

    character_statuses = relationship(
        "CharacterStatus",
        back_populates="user",
        cascade="all, delete-orphan",
    )

    user_buffs = relationship(
        "UserBuff",
        back_populates="user",
        cascade="all, delete-orphan",
    )

    user_items = relationship(
        "UserItem",
        back_populates="user",
        cascade="all, delete-orphan",
    )

    user_quests = relationship(
        "UserQuest",
        back_populates="user",
        cascade="all, delete-orphan",
    )

    usage_logs = relationship(
        "PhoneUsageLog",
        back_populates="user",
        cascade="all, delete-orphan",
    )

    ai_feedbacks = relationship(
        "AIFeedbackLog",
        back_populates="user",
        cascade="all, delete-orphan",
    )

    offline_reward_boxes = relationship(
        "OfflineRewardBox",
        back_populates="user",
        cascade="all, delete-orphan",
    )


class UserStatus(Base):
    __tablename__ = "user_status"

    user_id = Column(
        BigInteger,
        ForeignKey("users.user_id", ondelete="CASCADE"),
        primary_key=True,
    )

    current_character_id = Column(
        BigInteger,
        ForeignKey("character_info.character_id", ondelete="RESTRICT"),
        nullable=False,
    )

    player_level = Column(Integer, default=1)
    player_exp = Column(Integer, default=0)
    required_exp = Column(Integer, default=1000)

    hp_upgrade_lvl = Column(Integer, nullable=False, default=0)
    attack_upgrade_lvl = Column(Integer, nullable=False, default=0)
    defense_upgrade_lvl = Column(Integer, nullable=False, default=0)

    max_hp = Column(Integer, nullable=False, default=100)
    attack_power = Column(Integer, nullable=False, default=10)
    defense_power = Column(Integer, nullable=False, default=5)

    current_stage = Column(Integer, default=1)
    max_cleared_stage = Column(Integer, nullable=False, default=0)
    total_boss_kill_count = Column(Integer, default=0)
    updated_at = Column(DateTime, server_default=func.now(), onupdate=func.now())

    __table_args__ = (
        CheckConstraint(
            "max_hp > 0 AND attack_power >= 0 AND defense_power >= 0",
            name="chk_user_status_stats",
        ),
    )

    user = relationship("User", back_populates="status")
    current_character = relationship("CharacterInfo")


class CharacterStatus(Base):
    __tablename__ = "character_status"

    user_id = Column(
        BigInteger,
        ForeignKey("users.user_id", ondelete="CASCADE"),
        primary_key=True,
    )

    character_id = Column(
        BigInteger,
        ForeignKey("character_info.character_id", ondelete="CASCADE"),
        primary_key=True,
    )

    user = relationship("User", back_populates="character_statuses")
    character = relationship("CharacterInfo", back_populates="character_statuses")


class BuffInfo(Base):
    __tablename__ = "buff_info"

    buff_id = Column(BigInteger, primary_key=True, autoincrement=True)

    buff_type = Column(String(30), nullable=False)
    condition_grade = Column(String(20), nullable=False)
    buff_name = Column(String(100), nullable=False)

    effect_value = Column(Numeric(10, 2), nullable=False, default=0)
    is_decaying = Column(Boolean, nullable=False, default=True)
    decay_value = Column(Numeric(10, 2), nullable=False, default=0)

    __table_args__ = (
        UniqueConstraint("buff_type", "condition_grade", name="uq_buff_type_grade"),
        CheckConstraint(
            "buff_type IN ('ACTIVITY', 'RESTRAINT', 'QUEST', 'OFFLINE')",
            name="chk_buff_type",
        ),
        CheckConstraint(
            "condition_grade IN ('NORMAL', 'GOOD', 'BEST')",
            name="chk_condition_grade",
        ),
        CheckConstraint("effect_value >= 0", name="chk_effect_value"),
        CheckConstraint("is_decaying IN (0, 1)", name="chk_is_decaying"),
        CheckConstraint("decay_value >= 0", name="chk_decay_value"),
    )

    user_buffs = relationship("UserBuff", back_populates="buff_info")


class UserBuff(Base):
    __tablename__ = "user_buff"

    user_id = Column(
        BigInteger,
        ForeignKey("users.user_id", ondelete="CASCADE"),
        primary_key=True,
    )

    buff_date = Column(Date, primary_key=True)
    buff_type = Column(String(30), primary_key=True)

    buff_id = Column(
        BigInteger,
        ForeignKey("buff_info.buff_id", ondelete="CASCADE"),
        nullable=False,
    )

    condition_score = Column(Integer, nullable=False)
    current_effect_value = Column(Numeric(10, 2), nullable=False)
    is_active = Column(Boolean, nullable=False, default=True)

    applied_at = Column(DateTime, nullable=False, server_default=func.now())
    updated_at = Column(DateTime, nullable=False, server_default=func.now(), onupdate=func.now())

    __table_args__ = (
        CheckConstraint(
            "buff_type IN ('ACTIVITY', 'RESTRAINT', 'QUEST', 'OFFLINE')",
            name="chk_user_buff_type",
        ),
        CheckConstraint(
            "condition_score BETWEEN 0 AND 100",
            name="chk_condition_score",
        ),
        CheckConstraint(
            "current_effect_value >= 0",
            name="chk_current_effect_value",
        ),
        CheckConstraint(
            "is_active IN (0, 1)",
            name="chk_user_buff_active",
        ),
    )

    user = relationship("User", back_populates="user_buffs")
    buff_info = relationship("BuffInfo", back_populates="user_buffs")


class Item(Base):
    __tablename__ = "item"

    item_id = Column(BigInteger, primary_key=True, autoincrement=True)
    item_key = Column(String(100), nullable=False, unique=True)
    item_name = Column(String(100), nullable=False)
    item_type = Column(String(20), nullable=False)
    grade = Column(String(20), default="NORMAL")
    image_key = Column(String(100), nullable=False)

    base_attack = Column(Integer, default=0)
    base_defense = Column(Integer, default=0)

    user_items = relationship("UserItem", back_populates="item")


class UserItem(Base):
    __tablename__ = "user_item"

    user_item_id = Column(BigInteger, primary_key=True, autoincrement=True)

    user_id = Column(
        BigInteger,
        ForeignKey("users.user_id", ondelete="CASCADE"),
        nullable=False,
    )

    item_id = Column(
        BigInteger,
        ForeignKey("item.item_id", ondelete="CASCADE"),
        nullable=False,
    )

    enhance_level = Column(Integer, nullable=False, default=1)
    is_equipped = Column(Boolean, nullable=False, default=False)
    quantity = Column(Integer, nullable=False, default=1)

    __table_args__ = (
        UniqueConstraint("user_id", "item_id", name="uq_user_item"),
        CheckConstraint(
            "enhance_level >= 1",
            name="chk_user_item_enhance_level_positive",
        ),
        CheckConstraint(
            "quantity >= 0",
            name="chk_user_item_quantity_nonnegative",
        ),
    )

    user = relationship("User", back_populates="user_items")
    item = relationship("Item", back_populates="user_items")


class UserCurrency(Base):
    __tablename__ = "user_currency"

    user_id = Column(
        BigInteger,
        ForeignKey("users.user_id", ondelete="CASCADE"),
        primary_key=True,
    )

    gold = Column(BigInteger, default=0)
    gem = Column(BigInteger, default=0)
    updated_at = Column(DateTime, server_default=func.now(), onupdate=func.now())

    user = relationship("User", back_populates="currency")


class PhoneUsageLog(Base):
    __tablename__ = "phone_usage_log"

    usage_log_id = Column(BigInteger, primary_key=True, autoincrement=True)

    user_id = Column(
        BigInteger,
        ForeignKey("users.user_id", ondelete="CASCADE"),
        nullable=False,
    )

    usage_date = Column(Date, nullable=False)
    total_screen_minutes = Column(Integer, default=0)
    created_at = Column(DateTime, server_default=func.now())

    user = relationship("User", back_populates="usage_logs")
    ai_feedbacks = relationship("AIFeedbackLog", back_populates="usage_log")


class Quest(Base):
    __tablename__ = "quest"

    quest_id = Column(BigInteger, primary_key=True, autoincrement=True)

    # 공통 / 하 / 중 / 상
    quest_type = Column(String(100), nullable=False)

    # BattleWin, Phone_use, PlayTime, GoldDun, Stat
    quest_event = Column(String(50), nullable=False, default="NONE")

    # Unity에 표시할 퀘스트 문구
    quest_description = Column(Text, nullable=True)

    # 전날 완료 개수 계산용 퀘스트 여부
    is_condition_check = Column(Boolean, default=False)

    target_value = Column(Integer, nullable=False)
    reward_gold = Column(Integer, default=0)
    reward_gem = Column(Integer, default=0)
    is_active = Column(Boolean, default=True)

    user_quests = relationship("UserQuest", back_populates="quest")


class UserQuest(Base):
    __tablename__ = "user_quest"

    user_quest_id = Column(BigInteger, primary_key=True, autoincrement=True)

    user_id = Column(
        BigInteger,
        ForeignKey("users.user_id", ondelete="CASCADE"),
        nullable=False,
    )

    quest_id = Column(
        BigInteger,
        ForeignKey("quest.quest_id", ondelete="CASCADE"),
        nullable=False,
    )

    current_value = Column(Integer, nullable=False, default=0)

    is_completed = Column(Boolean, nullable=False, default=False)
    is_reward_claimed = Column(Boolean, nullable=False, default=False)

    assigned_date = Column(Date, nullable=True)
    completed_at = Column(DateTime, nullable=True)

    user = relationship("User", back_populates="user_quests")
    quest = relationship("Quest", back_populates="user_quests")


class OfflineRewardBox(Base):
    __tablename__ = "offline_reward_box"

    reward_box_id = Column(BigInteger, primary_key=True, autoincrement=True)

    user_id = Column(
        BigInteger,
        ForeignKey("users.user_id", ondelete="CASCADE"),
        nullable=False,
    )

    accumulated_min = Column(Integer, default=0)
    boss_kill_count = Column(Integer, default=0)
    reward_gold = Column(Integer, default=0)
    reward_gem = Column(Integer, default=0)
    is_claimed = Column(Boolean, default=False)
    created_at = Column(DateTime, server_default=func.now())
    claimed_at = Column(DateTime, nullable=True)

    user = relationship("User", back_populates="offline_reward_boxes")


class AIFeedbackLog(Base):
    __tablename__ = "ai_feedback_log"

    feedback_id = Column(BigInteger, primary_key=True, autoincrement=True)

    user_id = Column(
        BigInteger,
        ForeignKey("users.user_id", ondelete="CASCADE"),
        nullable=False,
    )

    usage_log_id = Column(
        BigInteger,
        ForeignKey("phone_usage_log.usage_log_id", ondelete="SET NULL"),
        nullable=True,
    )

    feedback_content = Column(Text, nullable=False)
    pattern_summary = Column(Text, nullable=True)
    previous_condition_quest_completed = Column(Integer, default=0)
    condition_result = Column(String(20), default="NORMAL")
    created_at = Column(DateTime, server_default=func.now())

    __table_args__ = (
        CheckConstraint(
            "previous_condition_quest_completed BETWEEN 0 AND 5",
            name="chk_previous_condition_quest_completed",
        ),
    )

    user = relationship("User", back_populates="ai_feedbacks")
    usage_log = relationship("PhoneUsageLog", back_populates="ai_feedbacks")
