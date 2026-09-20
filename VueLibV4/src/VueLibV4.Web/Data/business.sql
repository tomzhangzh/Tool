-- ============================================================
-- VueLibV4  BusinessDb  业务库初始化脚本（SQLite，可重复执行）
-- 由 Seeder 在每次启动时幂等执行：
--   * CREATE TABLE IF NOT EXISTS
--   * INSERT ... SELECT ... WHERE NOT EXISTS
-- ============================================================

-- ---------------- 学生表（免模型 CRUD Demo） ----------------
CREATE TABLE IF NOT EXISTS Student (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    Name        TEXT NOT NULL,
    Gender      TEXT NULL,           -- M / F（平台字典 gender）
    Age         INTEGER NULL,
    ClassName   TEXT NULL,
    Score       REAL NULL,
    Phone       TEXT NULL,
    Address     TEXT NULL,
    CreateText  TEXT NULL
);

-- ---------------- 课程表 ----------------
CREATE TABLE IF NOT EXISTS Course (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    Name        TEXT NOT NULL,
    Teacher     TEXT NULL,
    Credit      REAL NULL,
    ClassName   TEXT NULL,
    Remark      TEXT NULL
);

-- ---------------- 业务字典（业务库内字典，区别于平台 DynDict） ----------------
CREATE TABLE IF NOT EXISTS BusinessDict (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    DictType    TEXT NOT NULL,
    DictCode    TEXT NOT NULL,
    DictName    TEXT NULL,
    SortNo      INTEGER NOT NULL DEFAULT 0,
    IsActive    INTEGER NOT NULL DEFAULT 1,
    Remark      TEXT NULL
);

-- ============================================================
-- 种子数据（幂等）
-- ============================================================

INSERT INTO Student (Name, Gender, Age, ClassName, Score, Phone, Address, CreateText)
SELECT '张伟', 'M', 16, '高一(1)班', 88.5, '13800010001', '北京市朝阳区建国路1号', '2025-09-01 08:00:00'
WHERE NOT EXISTS (SELECT 1 FROM Student WHERE Name='张伟' AND ClassName='高一(1)班');
INSERT INTO Student (Name, Gender, Age, ClassName, Score, Phone, Address, CreateText)
SELECT '王芳', 'F', 15, '高一(1)班', 92.0, '13800010002', '北京市海淀区中关村大街2号', '2025-09-01 08:00:00'
WHERE NOT EXISTS (SELECT 1 FROM Student WHERE Name='王芳' AND ClassName='高一(1)班');
INSERT INTO Student (Name, Gender, Age, ClassName, Score, Phone, Address, CreateText)
SELECT '李娜', 'F', 16, '高一(1)班', 76.5, '13800010003', '北京市西城区西直门外3号', '2025-09-01 08:00:00'
WHERE NOT EXISTS (SELECT 1 FROM Student WHERE Name='李娜' AND ClassName='高一(1)班');
INSERT INTO Student (Name, Gender, Age, ClassName, Score, Phone, Address, CreateText)
SELECT '刘强', 'M', 17, '高一(2)班', 81.0, '13800010004', '北京市东城区东单北大街4号', '2025-09-01 08:00:00'
WHERE NOT EXISTS (SELECT 1 FROM Student WHERE Name='刘强' AND ClassName='高一(2)班');
INSERT INTO Student (Name, Gender, Age, ClassName, Score, Phone, Address, CreateText)
SELECT '陈杰', 'M', 16, '高一(2)班', 95.0, '13800010005', '北京市丰台区南三环5号', '2025-09-01 08:00:00'
WHERE NOT EXISTS (SELECT 1 FROM Student WHERE Name='陈杰' AND ClassName='高一(2)班');
INSERT INTO Student (Name, Gender, Age, ClassName, Score, Phone, Address, CreateText)
SELECT '杨洋', 'F', 15, '高一(2)班', 84.5, '13800010006', '北京市石景山区八角路6号', '2025-09-01 08:00:00'
WHERE NOT EXISTS (SELECT 1 FROM Student WHERE Name='杨洋' AND ClassName='高一(2)班');
INSERT INTO Student (Name, Gender, Age, ClassName, Score, Phone, Address, CreateText)
SELECT '赵敏', 'F', 16, '高二(1)班', 90.0, '13800010007', '北京市通州区新华大街7号', '2025-09-01 08:00:00'
WHERE NOT EXISTS (SELECT 1 FROM Student WHERE Name='赵敏' AND ClassName='高二(1)班');
INSERT INTO Student (Name, Gender, Age, ClassName, Score, Phone, Address, CreateText)
SELECT '黄磊', 'M', 17, '高二(1)班', 73.5, '13800010008', '北京市昌平区府学路8号', '2025-09-01 08:00:00'
WHERE NOT EXISTS (SELECT 1 FROM Student WHERE Name='黄磊' AND ClassName='高二(1)班');
INSERT INTO Student (Name, Gender, Age, ClassName, Score, Phone, Address, CreateText)
SELECT '周婷', 'F', 16, '高二(1)班', 87.0, '13800010009', '北京市大兴区兴华大街9号', '2025-09-01 08:00:00'
WHERE NOT EXISTS (SELECT 1 FROM Student WHERE Name='周婷' AND ClassName='高二(1)班');
INSERT INTO Student (Name, Gender, Age, ClassName, Score, Phone, Address, CreateText)
SELECT '吴昊', 'M', 18, '高二(2)班', 69.5, '13800010010', '北京市顺义区府前中街10号', '2025-09-01 08:00:00'
WHERE NOT EXISTS (SELECT 1 FROM Student WHERE Name='吴昊' AND ClassName='高二(2)班');
INSERT INTO Student (Name, Gender, Age, ClassName, Score, Phone, Address, CreateText)
SELECT '郑爽', 'F', 17, '高二(2)班', 98.0, '13800010011', '北京市房山区良乡西路11号', '2025-09-01 08:00:00'
WHERE NOT EXISTS (SELECT 1 FROM Student WHERE Name='郑爽' AND ClassName='高二(2)班');
INSERT INTO Student (Name, Gender, Age, ClassName, Score, Phone, Address, CreateText)
SELECT '孙浩', 'M', 17, '高二(2)班', 82.5, '13800010012', '北京市门头沟区新桥大街12号', '2025-09-01 08:00:00'
WHERE NOT EXISTS (SELECT 1 FROM Student WHERE Name='孙浩' AND ClassName='高二(2)班');

-- ---------------- 课程种子 ----------------
INSERT INTO Course (Name, Teacher, Credit, ClassName, Remark)
SELECT '语文', '王老师', 4.0, '高一(1)班', NULL
WHERE NOT EXISTS (SELECT 1 FROM Course WHERE Name='语文' AND ClassName='高一(1)班');
INSERT INTO Course (Name, Teacher, Credit, ClassName, Remark)
SELECT '数学', '李老师', 5.0, '高一(1)班', NULL
WHERE NOT EXISTS (SELECT 1 FROM Course WHERE Name='数学' AND ClassName='高一(1)班');
INSERT INTO Course (Name, Teacher, Credit, ClassName, Remark)
SELECT '英语', '赵老师', 4.0, '高一(2)班', NULL
WHERE NOT EXISTS (SELECT 1 FROM Course WHERE Name='英语' AND ClassName='高一(2)班');
INSERT INTO Course (Name, Teacher, Credit, ClassName, Remark)
SELECT '物理', '钱老师', 3.5, '高二(1)班', NULL
WHERE NOT EXISTS (SELECT 1 FROM Course WHERE Name='物理' AND ClassName='高二(1)班');

-- ---------------- 业务字典种子 ----------------
INSERT INTO BusinessDict (DictType, DictCode, DictName, SortNo, IsActive)
SELECT 'gender', 'M', '男', 1, 1
WHERE NOT EXISTS (SELECT 1 FROM BusinessDict WHERE DictType='gender' AND DictCode='M');
INSERT INTO BusinessDict (DictType, DictCode, DictName, SortNo, IsActive)
SELECT 'gender', 'F', '女', 2, 1
WHERE NOT EXISTS (SELECT 1 FROM BusinessDict WHERE DictType='gender' AND DictCode='F');
