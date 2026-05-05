# Idle Life Game FastAPI Backend

Unity 방치형 게임 프로토타입용 FastAPI + MySQL 백엔드입니다.  
이 버전은 `idle_game_db`의 12개 테이블 구조를 기준으로 작성되었습니다.

## 1. 기준 테이블

```text
users
character_type
character_status
character_condition
user_currency
item
user_item
quest
user_quest
phone_usage_log
ai_feedback_log
offline_reward_box
```

## 2. 설치

```bash
pip install -r requirements.txt
```

## 3. 환경변수 설정

`.env.example`을 복사해서 `.env` 파일을 만듭니다.

Windows:

```bash
copy .env.example .env
```

macOS/Linux:

```bash
cp .env.example .env
```

`.env` 안의 `DB_PASSWORD`를 본인 MySQL 비밀번호로 수정합니다.

```env
DB_USER=root
DB_PASSWORD=본인비밀번호
DB_HOST=localhost
DB_PORT=3306
DB_NAME=idle_game_db
```

주의: 실제 `.env` 파일은 GitHub에 올리지 않습니다.

## 4. DB 준비
DB_PROTOTYPE import하기전!!
MySQL Workbench에서 `idle_game_db`를 만들고, 기존에 export한 dump 파일을 import합니다.

기본 DB 생성 SQL:

```sql
CREATE DATABASE IF NOT EXISTS idle_game_db
DEFAULT CHARACTER SET utf8mb4
COLLATE utf8mb4_unicode_ci;

USE idle_game_db;
```

## 5. 서버 실행

```bash
uvicorn main:app --reload
```

## 6. API 문서 접속

```text
http://127.0.0.1:8000/docs
```

## 7. Unity 접속 주소

Android 에뮬레이터에서 접근:

```text
http://10.0.2.2:8000
```

실제 스마트폰에서 접근하려면 FastAPI를 다음처럼 실행합니다.

```bash
uvicorn main:app --reload --host 0.0.0.0 --port 8000
```

Unity에서는 아래 형식으로 접속합니다.

```text
http://내_PC_IP주소:8000
```

## 8. 기본 테스트 순서

1. `/health` 확인
2. `/character-types/`에서 캐릭터 유형 4개 확인
3. `/users/`로 사용자 생성
4. `/users/{user_id}`로 자동 생성된 캐릭터 상태, 재화, 컨디션 확인
5. `/quests/`로 퀘스트 조회
6. `/users/{user_id}/assign-basic-quests/`로 기본 퀘스트 부여
7. `/usage-logs/`로 폰 사용시간 기록
8. `/ai-feedbacks/`로 AI 피드백 기록
