import os
import pymysql
import re

DB_CONFIG = {
    'host': os.getenv('DB_HOST', 'YOUR_HOST'),
    'port': int(os.getenv('DB_PORT', 3306)),
    'user': os.getenv('DB_USER', 'YOUR_USER'),
    'password': os.getenv('DB_PASSWORD', 'YOUR_PASSWORD'),
    'database': os.getenv('DB_NAME', 'defaultdb'),
    'ssl': {'ssl': True},
    'charset': 'utf8mb4',
    'autocommit': True
}

def run_migration():
    print("Connecting to Aiven MySQL...")
    conn = pymysql.connect(**DB_CONFIG)
    cursor = conn.cursor()
    print("Connected successfully!")

    with open('database/schema.sql', 'r', encoding='utf-8') as f:
        sql_content = f.read()

    # Split SQL statements
    # Remove comments and split by semicolon
    statements = []
    current = []
    for line in sql_content.splitlines():
        trimmed = line.strip()
        if trimmed.startswith('--') or not trimmed:
            continue
        current.append(line)
        if trimmed.endswith(';'):
            stmt = '\n'.join(current).strip()
            if stmt:
                statements.append(stmt)
            current = []

    print(f"Executing {len(statements)} statements...")
    for i, stmt in enumerate(statements, 1):
        try:
            cursor.execute(stmt)
        except Exception as e:
            print(f"[Error at statement {i}]: {e}\nStatement:\n{stmt}")
            raise e

    print("All statements executed successfully!")

    # Verify tables
    cursor.execute("SHOW TABLES;")
    tables = [row[0] for row in cursor.fetchall()]
    print(f"\nTotal tables created: {len(tables)}")
    for t in sorted(tables):
        cursor.execute(f"SELECT COUNT(*) FROM `{t}`;")
        count = cursor.fetchone()[0]
        print(f" - {t}: {count} records")

    conn.close()
    print("\nDatabase initialization completed on Aiven MySQL!")

if __name__ == '__main__':
    run_migration()
