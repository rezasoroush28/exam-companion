"""Populate the fixed MVP challenge pool. This is an offline maintenance tool only."""

import argparse
import datetime as dt
import json
import os
import sqlite3
import urllib.request

API_URL = "https://bt.zoomeracademy.ir/api/v1/operator/questions/questions"
FIELD_GRADE_ID = 8
LESSON_ID = 87
LESSON_TITLE = "زیست‌شناسی 1"
TOPIC_IDS = [4253, 4319, 4346, 4399, 4457]
TARGET_PER_TOPIC = 5


def fetch_questions(token: str, topic_id: int, quantity: int) -> list[dict]:
    payload = json.dumps({
        "lesson_id": LESSON_ID,
        "field_grade_id": FIELD_GRADE_ID,
        "qty": quantity,
        "topic_ids": [topic_id],
    }).encode("utf-8")
    request = urllib.request.Request(
        f"{API_URL}?api_token={token}", payload,
        {"Content-Type": "application/json", "Accept": "application/json"}, method="POST")
    with urllib.request.urlopen(request, timeout=90) as response:
        body = json.loads(response.read().decode("utf-8"))
    return body.get("data") or []


def choose_varied(questions: list[dict], count: int) -> list[dict]:
    unique = {q.get("question_id"): q for q in questions if q.get("question_id")}.values()
    groups: dict[str, list[dict]] = {}
    for question in unique:
        groups.setdefault((question.get("level") or "unknown").strip().lower(), []).append(question)
    selected: list[dict] = []
    for level in ("easy", "medium", "hard", "very hard", "veryhard", "unknown"):
        if groups.get(level) and len(selected) < count:
            selected.append(groups[level].pop(0))
    remaining = [q for values in groups.values() for q in values]
    selected.extend(remaining[:max(0, count - len(selected))])
    return selected[:count]


def initialize(connection: sqlite3.Connection) -> None:
    connection.executescript("""
    CREATE TABLE IF NOT EXISTS mvp_challenge_config (
        config_id INTEGER PRIMARY KEY CHECK (config_id = 1),
        field_grade_id INTEGER NOT NULL,
        lesson_id INTEGER NOT NULL,
        lesson_title TEXT NOT NULL,
        updated_at_utc TEXT NOT NULL
    );
    CREATE TABLE IF NOT EXISTS mvp_challenge_topics (
        position INTEGER PRIMARY KEY,
        topic_id INTEGER NOT NULL UNIQUE,
        topic_title TEXT NOT NULL,
        importance REAL NOT NULL CHECK (importance >= 0 AND importance <= 1)
    );
    CREATE TABLE IF NOT EXISTS mvp_challenge_questions (
        question_id INTEGER PRIMARY KEY,
        topic_id INTEGER NOT NULL,
        title_html TEXT NOT NULL,
        answers_json TEXT NOT NULL,
        level TEXT,
        answer_key TEXT NOT NULL,
        descriptive_answer_html TEXT,
        question_json TEXT NOT NULL,
        fetched_at_utc TEXT NOT NULL,
        FOREIGN KEY (topic_id) REFERENCES mvp_challenge_topics(topic_id) ON DELETE CASCADE
    );
    CREATE INDEX IF NOT EXISTS idx_mvp_questions_topic_level
        ON mvp_challenge_questions(topic_id, level);
    """)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--topics-db", default="data/QuestionBankTopics.sqlite")
    parser.add_argument("--questions-db", default="data/QuestionBankOneQuestionPerTopic.sqlite")
    parser.add_argument("--token", default=os.environ.get("QUESTION_BANK_API_TOKEN"))
    parser.add_argument("--fetch-qty", type=int, default=30)
    args = parser.parse_args()
    if not args.token:
        raise SystemExit("Set QUESTION_BANK_API_TOKEN before running this offline importer.")

    topics_db = sqlite3.connect(args.topics_db)
    topics_db.row_factory = sqlite3.Row
    placeholders = ",".join("?" for _ in TOPIC_IDS)
    topic_rows = topics_db.execute(
        f"SELECT topic_id,value,importance,lesson_id FROM topics WHERE topic_id IN ({placeholders})", TOPIC_IDS).fetchall()
    topics_db.close()
    if len(topic_rows) != len(TOPIC_IDS) or any(row["lesson_id"] != LESSON_ID for row in topic_rows):
        raise SystemExit("Fixed MVP topics are missing or do not all belong to lesson 87.")
    topics = {row["topic_id"]: row for row in topic_rows}

    fetched_at = dt.datetime.now(dt.timezone.utc).replace(microsecond=0).isoformat()
    downloaded = {topic_id: choose_varied(fetch_questions(args.token, topic_id, args.fetch_qty), TARGET_PER_TOPIC)
                  for topic_id in TOPIC_IDS}

    connection = sqlite3.connect(args.questions_db)
    connection.execute("PRAGMA foreign_keys=ON")
    initialize(connection)
    with connection:
        connection.execute("DELETE FROM mvp_challenge_questions")
        connection.execute("DELETE FROM mvp_challenge_topics")
        connection.execute("DELETE FROM mvp_challenge_config")
        connection.execute("INSERT INTO mvp_challenge_config VALUES (1,?,?,?,?)",
                           (FIELD_GRADE_ID, LESSON_ID, LESSON_TITLE, fetched_at))
        for position, topic_id in enumerate(TOPIC_IDS, 1):
            topic = topics[topic_id]
            connection.execute("INSERT INTO mvp_challenge_topics VALUES (?,?,?,?)",
                               (position, topic_id, topic["value"], topic["importance"]))
            for question in downloaded[topic_id]:
                answers = question.get("answers") or []
                if len(answers) != 4 or not question.get("title") or not question.get("answer_key"):
                    continue
                connection.execute("""
                    INSERT OR REPLACE INTO mvp_challenge_questions
                    (question_id,topic_id,title_html,answers_json,level,answer_key,
                     descriptive_answer_html,question_json,fetched_at_utc)
                    VALUES (?,?,?,?,?,?,?,?,?)""", (
                    question["question_id"], topic_id, question["title"],
                    json.dumps(answers, ensure_ascii=False), question.get("level"),
                    str(question["answer_key"]), question.get("descriptive_answer"),
                    json.dumps(question, ensure_ascii=False), fetched_at))
    for row in connection.execute("""
        SELECT t.position,t.topic_id,t.topic_title,t.importance,COUNT(q.question_id) question_count,
               GROUP_CONCAT(DISTINCT COALESCE(q.level,'unknown')) levels
        FROM mvp_challenge_topics t LEFT JOIN mvp_challenge_questions q ON q.topic_id=t.topic_id
        GROUP BY t.position ORDER BY t.position"""):
        print(row)
    connection.close()


if __name__ == "__main__":
    main()
