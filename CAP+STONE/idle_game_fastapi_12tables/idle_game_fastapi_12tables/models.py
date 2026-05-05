from sqlalchemy import (
    Column,
    BigInteger,
    Integer,
    String,
    Date,
    DateTime,
    ForeignKey,
    Text,
    Boolean,
    CheckConstraint,
    UniqueConstraint,
)
from sqlalchemy.orm import relationship
from sqlalchemy.sql import func
from database import Base


class User(Base):
    __tablename__ = "users"

    user_id = Column(BigInteger, primary_key=True, autoincrement=True, index=True)
    email = Column(String(100), nullable=False, unique=True)
    password_hash = Column(String(255), nullable=False)
    nickname = Column(String(50), nullable=False)
    created_at = Column(DateTime, server_default=func.now())
    last_login_at = Column(DateTime, nullable=True)

    character_status = relationship(
        "CharacterStatus",
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
    conditions = relationship(
        "CharacterCondition",
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


class CharacterType(Base):
    __tablename__ = "character_type"

    character_type_id = Column(Integer, primary_key=True, autoincrement=True)
    type_code = Column(String(30), nullable=False, unique=True)
    type_name = Column(String(30), nullable=False)
    description = Column(Text, nullable=True)
    main_effect = Column(Text, nullable=True)

    character_statuses = relationship("CharacterStatus", back_populates="character_type")
    conditions = relationship("CharacterCondition", back_populates="character_type")


class CharacterStatus(Base):
    __tablename__ = "character_status"

    character_id = Column(BigInteger, primary_key=True, autoincrement=True)
    user_id = Column(BigInteger, ForeignKey("users.user_id", ondelete="CASCADE"), nullable=False)
    character_type_id = Column(Integer, ForeignKey("character_type.character_type_id"), nullable=False)
    level = Column(Integer, default=1)
    exp = Column(Integer, default=0)
    required_exp = Column(Integer, default=100)
    attack_power = Column(Integer, default=10)
    defense_power = Column(Integer, default=5)
    current_stage = Column(Integer, default=1)
    total_boss_kill_count = Column(Integer, default=0)
    updated_at = Column(DateTime, server_default=func.now(), onupdate=func.now())

    user = relationship("User", back_populates="character_status")
    character_type = relationship("CharacterType", back_populates="character_statuses")


class CharacterCondition(Base):
    __tablename__ = "character_condition"

    condition_id = Column(BigInteger, primary_key=True, autoincrement=True)
    user_id = Column(BigInteger, ForeignKey("users.user_id", ondelete="CASCADE"), nullable=False)
    character_type_id = Column(Integer, ForeignKey("character_type.character_type_id"), nullable=False)
    condition_score = Column(Integer, default=3)
    condition_grade = Column(String(20), default="NORMAL")
    last_updated_date = Column(Date, nullable=False)

    __table_args__ = (
        UniqueConstraint("user_id", "character_type_id", name="uq_user_character_condition"),
        CheckConstraint("condition_score BETWEEN 1 AND 5", name="chk_condition_score"),
    )

    user = relationship("User", back_populates="conditions")
    character_type = relationship("CharacterType", back_populates="conditions")


class UserCurrency(Base):
    __tablename__ = "user_currency"

    currency_id = Column(BigInteger, primary_key=True, autoincrement=True)
    user_id = Column(BigInteger, ForeignKey("users.user_id", ondelete="CASCADE"), nullable=False)
    gold = Column(BigInteger, default=0)
    gem = Column(BigInteger, default=0)
    updated_at = Column(DateTime, server_default=func.now(), onupdate=func.now())

    user = relationship("User", back_populates="currency")


class Item(Base):
    __tablename__ = "item"

    item_id = Column(BigInteger, primary_key=True, autoincrement=True)
    item_name = Column(String(100), nullable=False)
    item_type = Column(String(30), nullable=False)
    grade = Column(String(30), default="NORMAL")
    base_attack = Column(Integer, default=0)
    base_defense = Column(Integer, default=0)
    base_effect = Column(Text, nullable=True)
    sell_price = Column(Integer, default=0)

    user_items = relationship("UserItem", back_populates="item")


class UserItem(Base):
    __tablename__ = "user_item"

    user_item_id = Column(BigInteger, primary_key=True, autoincrement=True)
    user_id = Column(BigInteger, ForeignKey("users.user_id", ondelete="CASCADE"), nullable=False)
    item_id = Column(BigInteger, ForeignKey("item.item_id", ondelete="CASCADE"), nullable=False)
    enhance_level = Column(Integer, default=0)
    is_equipped = Column(Boolean, default=False)
    obtained_at = Column(DateTime, server_default=func.now())

    user = relationship("User", back_populates="user_items")
    item = relationship("Item", back_populates="user_items")


class Quest(Base):
    __tablename__ = "quest"

    quest_id = Column(BigInteger, primary_key=True, autoincrement=True)
    quest_name = Column(String(100), nullable=False)
    quest_description = Column(Text, nullable=True)
    quest_type = Column(String(30), nullable=False)
    target_value = Column(Integer, nullable=False)
    reward_gold = Column(Integer, default=0)
    reward_gem = Column(Integer, default=0)
    reward_exp = Column(Integer, default=0)
    condition_recovery = Column(Integer, default=0)
    is_active = Column(Boolean, default=True)

    user_quests = relationship("UserQuest", back_populates="quest")


class UserQuest(Base):
    __tablename__ = "user_quest"

    user_quest_id = Column(BigInteger, primary_key=True, autoincrement=True)
    user_id = Column(BigInteger, ForeignKey("users.user_id", ondelete="CASCADE"), nullable=False)
    quest_id = Column(BigInteger, ForeignKey("quest.quest_id", ondelete="CASCADE"), nullable=False)
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
    user_id = Column(BigInteger, ForeignKey("users.user_id", ondelete="CASCADE"), nullable=False)
    usage_date = Column(Date, nullable=False)
    total_screen_minutes = Column(Integer, default=0)
    target_app_minutes = Column(Integer, default=0)
    night_usage_minutes = Column(Integer, default=0)
    is_life_pattern_good = Column(Boolean, default=True)
    created_at = Column(DateTime, server_default=func.now())

    __table_args__ = (
        UniqueConstraint("user_id", "usage_date", name="uq_user_usage_date"),
    )

    user = relationship("User", back_populates="usage_logs")
    ai_feedbacks = relationship("AIFeedbackLog", back_populates="usage_log")


class AIFeedbackLog(Base):
    __tablename__ = "ai_feedback_log"

    feedback_id = Column(BigInteger, primary_key=True, autoincrement=True)
    user_id = Column(BigInteger, ForeignKey("users.user_id", ondelete="CASCADE"), nullable=False)
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
    user_id = Column(BigInteger, ForeignKey("users.user_id", ondelete="CASCADE"), nullable=False)
    accumulated_seconds = Column(Integer, default=0)
    boss_kill_count = Column(Integer, default=0)
    reward_gold = Column(Integer, default=0)
    reward_exp = Column(Integer, default=0)
    is_claimed = Column(Boolean, default=False)
    created_at = Column(DateTime, server_default=func.now())
    claimed_at = Column(DateTime, nullable=True)

    user = relationship("User", back_populates="offline_reward_boxes")
