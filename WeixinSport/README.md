# 运动小达人 · 微信小程序

面向小学运动场景的微信小程序，支持 **老师 / 学生 / 家长** 三端角色，孩子自主打卡运动数据，系统自动进行 **周评选** 与 **月度明星** 评选，多元奖项激励孩子持续运动。

## 核心功能

### 角色
- **学生**：选择运动项目 → 填写时长 → 自动计算卡路里 → 打卡；查看本周/本月统计、奖项墙、班级排名
- **老师**：创建班级、获取邀请码；查看班级成员；周评选/月度明星一键生成；查看班级排名
- **家长**：绑定孩子账号；查看孩子的运动数据、奖项、排名

### 周评选（6 个奖项，让更多孩子获得成就感）
| 奖项 | 评选维度 |
|------|---------|
| 🔥 卡路里燃烧之星 | 本周累计消耗卡路里最多 |
| ⏱️ 运动时长之星 | 本周累计运动时长最长 |
| 📅 运动坚持之星 | 本周打卡天数最多 |
| 🎯 运动多面手 | 本周运动种类最丰富 |
| 📈 进步之星 | 本周比上周进步最大 |
| 🌅 早起运动之星 | 本周最早开始运动 |

每周日晚自动结算（也可由老师手动触发），每个奖项取前 3 名获奖。

### 月度明星计划
- 🏆 月度运动冠军 / 🥈 亚军 / 🥉 季军：综合得分前 3
- 💪 月度坚持之星：打卡满 20 天
- 🌟 月度进步之星：本月比上月进步最大

**综合得分** = 卡路里 30% + 时长 30% + 频率 25% + 多样性 15%（标准化后加权）

### 数据库集合
| 集合名 | 用途 |
|--------|------|
| `users` | 用户表：openid、role、name、avatar、weight、classIds、childOpenid、parentOpenids |
| `classes` | 班级表：name、code(邀请码)、teacherOpenid、status |
| `class_members` | 班级成员表：classId、openid、role、joinTime |
| `checkins` | 打卡记录：openid、exerciseId、duration、calorie、createTime、dateStr、weekKey、monthKey |
| `awards` | 奖项表：openid、classId、periodType(weekly/monthly/monthly_top/monthly_special)、periodKey、awardType、winners 等 |

### 云函数
| 云函数 | 功能 |
|--------|------|
| `login` | 登录、绑定角色、获取/更新个人资料 |
| `class` | 班级创建、加入、详情、成员、家长绑定孩子 |
| `checkin` | 打卡提交、今日、列表、删除 |
| `stats` | 周/月统计、班级排名 |
| `awards` | 周评选查询/生成、月度明星查询/生成、个人奖项墙 |

## 部署步骤

### 1. 准备
1. 注册微信小程序账号，获取 AppID
2. 在 [微信公众平台 → 开发管理 → 开发设置] 开启云开发
3. 创建一个云开发环境，记下环境 ID

### 2. 项目配置
1. 用微信开发者工具打开本项目
2. 打开 [miniprogram/app.js](miniprogram/app.js)，将 `env: 'weixin-sport-env'` 改为你的云开发环境 ID
3. 打开 [project.config.json](project.config.json)，将 `appid` 改为你的小程序 AppID

### 3. 部署云函数
在开发者工具左侧 `cloudfunctions` 目录下，依次右键每个云函数 → **上传并部署：云端安装依赖**：
- `login`
- `class`
- `checkin`
- `stats`
- `awards`

### 4. 创建数据库集合
进入云开发控制台 → 数据库，手动创建以下集合（无需建索引，命名严格一致）：
- `users`
- `classes`
- `class_members`
- `checkins`
- `awards`

> 权限设置：所有集合默认改为「仅创建者可读写」→ 改为 **「所有用户可读，仅创建者可写」** 或自定义安全规则。

推荐的安全规则（`checkins` 示例）：
```json
{
  "read": "doc.openid == auth.openid || get('users', auth.openid).role == 'teacher'",
  "write": "doc.openid == auth.openid"
}
```

### 5. 体验
1. 用一个微信号注册为「老师」→ 创建班级 → 复制邀请码
2. 另一个微信号注册为「学生」→ 输入邀请码加入 → 打卡
3. 老师在「周评选」「月度明星」页点击「生成」即可看到结果

## 自动结算（可选）
项目已实现老师手动触发生成。如需自动结算：
1. 在云开发控制台 → 云函数 → 定时触发器
2. 给 `awards` 云函数添加触发器：
   - 每周日 23:00 自动生成本周评选：Cron `0 0 23 * * 0`
   - 每月最后一天 23:00 生成月度明星：Cron `0 0 23 L * *`

并在 `awards/index.js` 的 main 入口判断 `event.Type === 'Timer'` 时调用 `calcWeekly` / `calcMonthly`（按当前周/月偏移 0）。

## 目录结构
```
WeixinSport/
├── miniprogram/
│   ├── app.js / app.json / app.wxss / sitemap.json
│   ├── components/
│   │   ├── award-card/
│   │   ├── stat-card/
│   │   └── empty-state/
│   ├── pages/
│   │   ├── index/        首页（角色分流）
│   │   ├── login/        登录/角色选择
│   │   ├── checkin/      运动打卡
│   │   ├── checkin-list/ 打卡记录
│   │   ├── weekly/       周评选
│   │   ├── monthly/      月度明星
│   │   ├── stats/        统计图表
│   │   ├── ranking/      班级排名
│   │   ├── class/        班级管理
│   │   ├── class-detail/ 班级详情
│   │   ├── awards/       奖项墙
│   │   └── profile/      个人中心
│   └── utils/
│       ├── api.js        云函数封装
│       ├── constants.js  常量
│       └── util.js       工具
└── cloudfunctions/
    ├── login/
    ├── class/
    ├── checkin/
    ├── stats/
    └── awards/
```

## 卡路里计算
卡路里 = MET × 体重(kg) × 时长(小时)，权重在 `utils/constants.js` 的 `EXERCISE_TYPES` 中维护（MET 值）。小学生默认体重 30kg，可在注册或个人中心修改。

## 后续可扩展
- 接入微信运动步数（`wx.getWeRunData`）作为补充数据源
- 老师端审核打卡记录（防止作弊）
- 班级间 PK / 校园榜
- 奖项电子证书导出与分享
- 提醒推送（未打卡提醒）
