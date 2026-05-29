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
    func,
)
from sqlalchemy.orm import relationship

from database import Base


class CharacterInfo(Base):
    __tablename__ = "character_info"

    character_id = Column(BigInteger, primary_key=True, autoincrement=True)
    character_key = Column(String(100), nullable=False, unique=True)
    character_name = Column(String(50), nullable=False)
    description = Column(Text, nullable=True)
    main_effect = Column(Text, nullable=True)


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

    character_conditions = relationship(
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
    required_exp = Column(Integer, default=100)

    current_stage = Column(Integer, default=1)
    total_boss_kill_count = Column(Integer, default=0)

    max_hp = Column(Integer, default=100)
    attack_power = Column(Integer, default=10)
    defense_power = Column(Integer, default=5)

    hp_upgrade_lvl = Column(Integer, default=1)
    attack_upgrade_lvl = Column(Integer, default=1)
    defense_upgrade_lvl = Column(Integer, default=1)

    updated_at = Column(DateTime, server_default=func.now(), onupdate=func.now())

    __table_args__ = (
        CheckConstraint(
            "max_hp >= 1 AND attack_power >= 0 AND defense_power >= 0",
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

    character_level = Column(Integer, default=1)

    user = relationship("User", back_populates="character_statuses")
    character = relationship("CharacterInfo")


class CharacterCondition(Base):
    __tablename__ = "character_condition"

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

    condition_score = Column(Integer, default=3)
    condition_grade = Column(String(20), default="NORMAL")
    last_updated_date = Column(Date, nullable=False)

    __table_args__ = (
        CheckConstraint(
            "condition_score BETWEEN 1 AND 3",
            name="chk_condition_score",
        ),
    )

    user = relationship("User", back_populates="character_conditions")
    character = relationship("CharacterInfo")


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

    quantity = Column(Integer, nullable=False, default=1)

    enhance_level = Column(Integer, default=0)
    is_equipped = Column(Boolean, default=False)

    __table_args__ = (
        CheckConstraint(
            "enhance_level BETWEEN 0 AND 5",
            name="chk_enhance_level",
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
    quest_name = Column(String(100), nullable=False)
    quest_description = Column(Text, nullable=True)

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

    progress_value = Column(Integer, default=0)
    is_completed = Column(Boolean, default=False)
    is_reward_claimed = Column(Boolean, default=False)
    assigned_date = Column(Date, nullable=False)
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
    reward_exp = Column(Integer, default=0)
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