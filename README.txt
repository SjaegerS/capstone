Idle Game FastAPI 로컬 실행 가이드
====================================

이 문서는 GitHub에 올라간 FastAPI 백엔드 코드를 다른 사용자가 자신의 로컬 환경에서 실행하기 위한 가이드입니다.

본 프로젝트는 다음 구조를 기준으로 합니다.

- FastAPI 백엔드
- MySQL 로컬 데이터베이스
- SQLAlchemy ORM
- PyMySQL
- python-dotenv
- Python 3.11.x 권장

주의:
.env 파일과 venv 폴더는 GitHub에 올리지 않습니다.
각 사용자는 자신의 로컬 환경에서 직접 생성해야 합니다.


1. GitHub에서 프로젝트 받기
----------------------------

터미널 또는 Git Bash에서 프로젝트를 받을 위치로 이동한 뒤 clone 또는 pull을 진행합니다.

예시:

    git clone <레포지토리_URL>

이미 프로젝트를 받은 상태라면:

    git pull

FastAPI 코드가 있는 폴더로 이동합니다.

예시:

    cd C:\github\capstone\CAP+STONE\idle_game_fastapi_12tables

이 폴더 안에 아래 파일들이 있어야 합니다.

    main.py
    models.py
    schemas.py
    database.py
    requirements.txt
    .env.example


2. Python 버전 확인
-------------------

Python 3.11.x 사용을 권장합니다.

확인 명령어:

    python --version

예상 예시:

    Python 3.11.x


3. 가상환경 생성
----------------
GIT 폴더말고 본인 로컬환경에서 복붙후 따라하기 바람
FastAPI 폴더에서 아래 명령어를 실행합니다.
파일탐색기 경로에 cmd 치고 실행하는 것임

    python -m venv venv

생성 후 폴더 구조 예시:

    idle_game_fastapi_12tables/
    ├─ main.py
    ├─ models.py
    ├─ schemas.py
    ├─ database.py
    ├─ requirements.txt
    ├─ .env.example
    └─ venv/


4. 가상환경 실행
----------------

Windows 기준:

    venv\Scripts\activate

성공하면 터미널 앞에 (venv)가 표시됩니다.

예시:

    (venv) C:\github\capstone\CAP+STONE\idle_game_fastapi_12tables>


5. 필요한 패키지 설치
---------------------

가상환경이 켜진 상태에서 실행합니다.
경로 앞에 (venv)붙어 있는거 확인하고 할것
    pip install -r requirements.txt

requirements.txt에는 보통 다음 패키지가 포함됩니다.

    fastapi
    uvicorn
    sqlalchemy
    pydantic
    pymysql
    python-dotenv
    email-validator


6. .env 파일 생성
-----------------

.env 파일은 GitHub에 올리지 않습니다.
각자 로컬에서 직접 만들어야 합니다.

.env.example 파일을 복사해서 .env 파일을 만듭니다.
password만 자기 root 비번으로
파일 구조:

    idle_game_fastapi_12tables/
    ├─ .env.example
    └─ .env

.env 파일 내용 예시:

    DB_USER=root
    DB_PASSWORD=자신의_MySQL_비밀번호
    DB_HOST=localhost
    DB_PORT=3306
    DB_NAME=idle_game_db

주의:
- 파일 이름은 반드시 .env 이어야 합니다.
- .env.txt가 되면 안 됩니다.
- DB_PASSWORD 뒤에 불필요한 공백을 넣지 않습니다.
- MySQL Workbench에서 root로 접속할 때 사용하는 비밀번호를 넣습니다.


7. MySQL 데이터베이스 생성
--------------------------

MySQL Workbench 또는 MySQL 콘솔에서 최신 DB 생성 SQL을 실행합니다.

DB 이름은 다음과 같아야 합니다.
DB 이름 이걸로 수정하거나 안에 코드 저걸로 되어있는거 본인 db이름으로 변경해야함 근데 db이름 아래로 추천
    idle_game_db

database.py의 기본 설정도 이 이름을 기준으로 되어 있습니다.

SQL 실행 후 아래 테이블들이 생성되어 있어야 합니다.

    character_info
    users
    character_status
    character_condition
    item
    user_item
    user_currency
    phone_usage_log
    quest
    user_quest
    offline_reward_box
    ai_feedback_log


8. FastAPI 서버 실행
--------------------

가상환경이 켜진 상태에서 FastAPI 폴더에서 실행합니다.

    uvicorn main:app --reload

정상 실행 예시:

    Uvicorn running on http://127.0.0.1:8000
    Application startup complete.

브라우저에서 아래 주소로 접속합니다.

    http://127.0.0.1:8000/docs


9. 기본 연결 확인
-----------------

Swagger UI에서 다음 API를 실행합니다.

    GET /health

정상 응답:

    {
      "status": "ok"
    }


10. 테스트 순서
---------------

처음 DB를 만든 직후에는 데이터가 비어 있습니다.
따라서 GET 요청 결과가 []로 나오는 것은 정상입니다.

기본 테스트 순서는 다음과 같습니다.

1) 캐릭터 생성

    POST /characters/

Request body 예시:

    {
      "character_key": "activity_character",
      "character_name": "활동형",
      "description": "기본 성장형 캐릭터",
      "main_effect": "기본 성장 효율 증가",
      "image_key": "character_activity"
    }

2) 캐릭터 목록 확인

    GET /characters/

3) 유저 생성

    POST /users/

Request body 예시:

    {
      "email": "test@example.com",
      "password_hash": "test_password_hash",
      "nickname": "테스트유저",
      "default_character_id": 1
    }

주의:
유저 생성 전에 character_info에 character_id = 1 캐릭터가 있어야 합니다.

4) 유저 상세 조회

    GET /users/1

5) 아이템 생성

    POST /items/

무기 예시:

    {
      "item_key": "weapon_old_sword",
      "item_name": "낡은 검",
      "item_type": "WEAPON",
      "grade": "NORMAL",
      "image_key": "weapon_old_sword",
      "base_attack": 5,
      "base_defense": 0,
      "base_effect": "기본 공격력 증가",
      "enhance_base_cost": 100
    }

방어구 예시:

    {
      "item_key": "armor_apprentice",
      "item_name": "수습자의 갑옷",
      "item_type": "ARMOR",
      "grade": "NORMAL",
      "image_key": "armor_apprentice",
      "base_attack": 0,
      "base_defense": 5,
      "base_effect": "기본 방어력 증가",
      "enhance_base_cost": 100
    }

주의:
item_type은 반드시 WEAPON 또는 ARMOR만 사용할 수 있습니다.
grade는 NORMAL, RARE, EPIC, UNIQUE, LEGENDARY 중 하나여야 합니다.

6) 유저에게 아이템 지급

    POST /user-items/

Request body 예시:

    {
      "user_id": 1,
      "item_id": 1,
      "enhance_level": 0,
      "is_equipped": true
    }

7) 골드 지급

    PATCH /users/1/currency/

Request body 예시:

    {
      "gold": 1000
    }

8) 아이템 강화

    PATCH /user-items/1/enhance/

강화 성공 시:
- user_currency.gold 감소
- user_item.enhance_level 증가

9) 휴대폰 사용시간 기록

    POST /usage-logs/

Request body 예시:

    {
      "user_id": 1,
      "usage_date": "2026-05-20",
      "total_screen_minutes": 245
    }

같은 날짜로 다시 요청하면 기존 기록을 갱신합니다.

10) 퀘스트 생성

    POST /quests/

Request body 예시:

    {
      "quest_key": "quest_phone_usage_under_240",
      "quest_name": "하루 사용시간 4시간 이하",
      "quest_description": "오늘 휴대폰 총 사용시간을 4시간 이하로 유지하세요.",
      "quest_type": "USAGE",
      "image_key": "quest_usage",
      "target_value": 240,
      "reward_gold": 500,
      "reward_exp": 80,
      "condition_recovery": 1,
      "is_active": true
    }

11) 유저에게 퀘스트 할당

    POST /user-quests/

Request body 예시:

    {
      "user_id": 1,
      "quest_id": 1,
      "progress_value": 0,
      "is_accepted": true,
      "is_completed": false,
      "is_reward_claimed": false,
      "assigned_date": "2026-05-20"
    }

12) 퀘스트 완료

    PATCH /user-quests/1/complete/

13) 퀘스트 보상 수령

    PATCH /user-quests/1/claim-reward/


11. 삭제 API
------------

테스트 중 잘못 넣은 데이터는 Swagger에서 삭제할 수 있습니다.

    DELETE /users/{user_id}
    DELETE /characters/{character_id}
    DELETE /items/{item_id}
    DELETE /quests/{quest_id}

주의:
현재 장착 중인 캐릭터는 삭제할 수 없도록 처리하는 것이 안전합니다.


12. 자주 발생하는 오류
----------------------

1) Access denied for user 'root'@'localhost' (using password: NO)

원인:
.env 파일을 읽지 못했거나 DB_PASSWORD가 비어 있습니다.

확인:
- .env 파일이 database.py와 같은 폴더에 있는지 확인
- 파일명이 .env.txt가 아닌 .env인지 확인
- DB_PASSWORD 값이 들어 있는지 확인

확인 명령어:

    python -c "from dotenv import load_dotenv; import os; load_dotenv(); print(os.getenv('DB_PASSWORD'))"

2) ModuleNotFoundError: No module named 'dotenv'

원인:
가상환경에 python-dotenv가 설치되지 않았습니다.

해결:

    venv\Scripts\activate
    pip install -r requirements.txt

또는:

    pip install python-dotenv

3) GET 요청 결과가 []

원인:
API 요청은 성공했지만 DB에 데이터가 없습니다.

해결:
먼저 POST 요청으로 데이터를 생성한 뒤 GET으로 조회합니다.

4) POST /users/ 404 Not Found

원인:
default_character_id에 해당하는 캐릭터가 character_info에 없습니다.

해결:
먼저 POST /characters/ 또는 SQL INSERT로 기본 캐릭터를 생성합니다.

5) Data truncated for column 'item_type'

원인:
item_type에 WEAPON 또는 ARMOR 외의 값을 넣었습니다.

해결:
item_type은 반드시 아래 중 하나만 사용합니다.

    WEAPON
    ARMOR


13. Git에 올리지 말아야 할 파일
-------------------------------
Git에는 오직 .env.example 파일만 있어야함 venv도 본인 환경 폴더에만 존재
다음 파일과 폴더는 GitHub에 올리지 않습니다.

    .env
    venv/
    __pycache__/
    *.pyc

.gitignore에 아래 내용을 추가합니다.

    venv/
    .env
    __pycache__/
    *.pyc


14. Git에 올려야 할 파일
------------------------

다음 파일은 GitHub에 올립니다.

    main.py
    models.py
    schemas.py
    database.py
    requirements.txt
    .env.example
    최신 DB 생성 SQL 파일
    README.txt


15. 실행 요약
-------------

처음 받은 사용자는 아래 순서대로 진행하면 됩니다.

    cd FastAPI_폴더
    python -m venv venv
    venv\Scripts\activate
    pip install -r requirements.txt

그다음:

    .env.example을 복사해서 .env 생성
    .env에 자신의 MySQL 비밀번호 입력
    MySQL에서 최신 SQL 실행
    uvicorn main:app --reload

브라우저 접속:

    http://127.0.0.1:8000/docs


16. 종료 방법
-------------

FastAPI 서버 종료:

    CTRL + C

가상환경 종료:

    deactivate
