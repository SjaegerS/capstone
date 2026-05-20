from sqlalchemy import (
    BigInteger,
    Boolean,
    CheckConstraint,
    Column,
    Date,
    DateTime,
    ForeignKey,
    Integer,
    String,
    Text,
    UniqueConstraint,
)
from sqlalchemy.orm import relationship
from sqlalchemy.sql import func

from database import Base


class CharacterInfo(Base):
    __tablename__ = "character_info"

    character_id = Column(BigInteger, primary_key=True, autoincrement=True)
    character_key = Column(String(100), nullable=False, unique=True)
    character_name = Column(String(50), nullable=False)
    description = Column(Text, nullable=True)
    main_effect = Column(Text, nullable=True)
    image_key = Column(String(100), nullable=False)

    current_users = relationship(
        "User",
        back_populates="current_character",
        foreign_keys="User.current_character_id",
    )

    statuses = relationship(
        "CharacterStatus",
        back_populates="character",
        cascade="all, delete-orphan",
    )

    conditions = relationship(
        "CharacterCondition",
        back_populates="character",
        cascade="all, delete-orphan",
    )


class User(Base):
    __tablename__ = "users"

    user_id = Column(BigInteger, primary_key=True, autoincrement=True, index=True)
    current_character_id = Column(
        BigInteger,
        ForeignKey("character_info.character_id", ondelete="RESTRICT"),
        nullable=False,
    )
    email = Column(String(100), nullable=False, unique=True)
    password_hash = Column(String(255), nullable=False)
    nickname = Column(String(50), nullable=False)
    created_at = Column(DateTime, server_default=func.now())
    last_login_at = Column(DateTime, nullable=True)

    current_character = relationship(
        "CharacterInfo",
        back_populates="current_users",
        foreign_keys=[current_character_id],
    )

    character_statuses = relationship(
        "CharacterStatus",
        back_populates="user",
        cascade="all, delete-orphan",
    )

    conditions = relationship(
        "CharacterCondition",
        back_populates="user",
        cascade="all, delete-orphan",
    )

    currency = relationship(
        "UserCurrency",
        back_populates="user",
        uselist=False,
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


class CharacterStatus(Base):
    __tablename__ = "character_status"

    status_id = Column(BigInteger, primary_key=True, autoincrement=True)
    user_id = Column(
        BigInteger,
        ForeignKey("users.user_id", ondelete="CASCADE"),
        nullable=False,
    )
    character_id = Column(
        BigInteger,
        ForeignKey("character_info.character_id", ondelete="CASCADE"),
        nullable=False,
    )

    level = Column(Integer, default=1)
    exp = Column(Integer, default=0)
    required_exp = Column(Integer, default=100)

    max_hp = Column(Integer, default=100)
    current_hp = Column(Integer, default=100)

    attack_power = Column(Integer, default=10)
    defense_power = Column(Integer, default=5)

    current_stage = Column(Integer, default=1)
    total_boss_kill_count = Column(Integer, default=0)
    updated_at = Column(DateTime, server_default=func.now(), onupdate=func.now())

    __table_args__ = (
        UniqueConstraint("user_id", "character_id", name="uq_user_character_status"),
        CheckConstraint(
            "current_hp >= 0 AND max_hp >= 1 AND current_hp <= max_hp",
            name="chk_character_hp",
        ),
    )

    user = relationship("User", back_populates="character_statuses")
    character = relationship("CharacterInfo", back_populates="statuses")


class CharacterCondition(Base):
    __tablename__ = "character_condition"

    condition_id = Column(BigInteger, primary_key=True, autoincrement=True)
    user_id = Column(
        BigInteger,
        ForeignKey("users.user_id", ondelete="CASCADE"),
        nullable=False,
    )
    character_id = Column(
        BigInteger,
        ForeignKey("character_info.character_id", ondelete="CASCADE"),
        nullable=False,
    )

    condition_score = Column(Integer, default=3)
    condition_grade = Column(String(20), default="NORMAL")
    last_updated_date = Column(Date, nullable=False)

    __table_args__ = (
        UniqueConstraint("user_id", "character_id", name="uq_user_character_condition"),
        CheckConstraint("condition_score BETWEEN 1 AND 3", name="chk_condition_score"),
    )

    user = relationship("User", back_populates="conditions")
    character = relationship("CharacterInfo", back_populates="conditions")


class UserCurrency(Base):
    __tablename__ = "user_currency"

    currency_id = Column(BigInteger, primary_key=True, autoincrement=True)
    user_id = Column(
        BigInteger,
        ForeignKey("users.user_id", ondelete="CASCADE"),
        nullable=False,
        unique=True,
    )
    gold = Column(BigInteger, default=0)
    updated_at = Column(DateTime, server_default=func.now(), onupdate=func.now())

    user = relationship("User", back_populates="currency")


class Item(Base):
    __tablename__ = "item"

    item_id = Column(BigInteger, primary_key=True, autoincrement=True)
    item_key = Column(String(100), nullable=False, unique=True)
    item_name = Column(String(100), nullable=False)
    item_type = Column(String(30), nullable=False)
    grade = Column(String(30), default="NORMAL")
    image_key = Column(String(100), nullable=False)

    base_attack = Column(Integer, default=0)
    base_defense = Column(Integer, default=0)
    base_effect = Column(Text, nullable=True)
    enhance_base_cost = Column(Integer, default=100)

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
    enhance_level = Column(Integer, default=0)
    is_equipped = Column(Boolean, default=False)
    obtained_at = Column(DateTime, server_default=func.now())

    user = relationship("User", back_populates="user_items")
    item = relationship("Item", back_populates="user_items")


class Quest(Base):
    __tablename__ = "quest"

    quest_id = Column(BigInteger, primary_key=True, autoincrement=True)
    quest_key = Column(String(100), nullable=False, unique=True)
    quest_name = Column(String(100), nullable=False)
    quest_description = Column(Text, nullable=True)
    quest_type = Column(String(30), nullable=False)
    image_key = Column(String(100), nullable=False)

    target_value = Column(Integer, nullable=False)
    reward_gold = Column(Integer, default=0)
    reward_exp = Column(Integer, default=0)
    condition_recovery = Column(Integer, default=0)
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
    progress_value = Column(Integer, default=0)
    is_accepted = Column(Boolean, default=False)
    is_completed = Column(Boolean, default=False)
    is_reward_claimed = Column(Boolean, default=False)
    assigned_date = Column(Date, nullable=False)
    completed_at = Column(DateTime, nullable=True)

    user = relationship("User", back_populates="user_quests")
    quest = relationship("Quest", back_populates="user_quests")


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

    __table_args__ = (
        UniqueConstraint("user_id", "usage_date", name="uq_user_usage_date"),
    )

    user = relationship("User", back_populates="usage_logs")
    ai_feedbacks = relationship("AIFeedbackLog", back_populates="usage_log")


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
    quest_suggestion = Column(Text, nullable=True)
    condition_result = Column(String(20), default="NORMAL")
    created_at = Column(DateTime, server_default=func.now())

    user = relationship("User", back_populates="ai_feedbacks")
    usage_log = relationship("PhoneUsageLog", back_populates="ai_feedbacks")


class OfflineRewardBox(Base):
    __tablename__ = "offline_reward_box"

    reward_box_id = Column(BigInteger, primary_key=True, autoincrement=True)
    user_id = Column(
        BigInteger,
        ForeignKey("users.user_id", ondelete="CASCADE"),
        nullable=False,
    )
    accumulated_seconds = Column(Integer, default=0)
    boss_kill_count = Column(Integer, default=0)
    reward_gold = Column(Integer, default=0)
    reward_exp = Column(Integer, default=0)
    is_claimed = Column(Boolean, default=False)
    created_at = Column(DateTime, server_default=func.now())
    claimed_at = Column(DateTime, nullable=True)

    user = relationship("User", back_populates="offline_reward_boxes")