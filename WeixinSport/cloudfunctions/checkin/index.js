// cloudfunctions/checkin/index.js
const cloud = require('wx-server-sdk');
cloud.init({ env: cloud.DYNAMIC_CURRENT_ENV });
const db = cloud.database();
const _ = db.command;

const calcCalorie = (met, durationMinutes, weight = 30) => {
  return Math.round(met * weight * (durationMinutes / 60));
};

const formatDate = (date, fmt = 'YYYY-MM-DD') => {
  const o = {
    'YYYY': date.getFullYear(),
    'MM': String(date.getMonth() + 1).padStart(2, '0'),
    'DD': String(date.getDate()).padStart(2, '0')
  };
  return fmt.replace(/YYYY|MM|DD/g, m => o[m]);
};

// 获取用户 ID：兼容小程序（OPENID）和 H5（event._uid）环境
const getUserId = (event) => {
  const wxCtx = cloud.getWXContext();
  if (wxCtx.OPENID) return wxCtx.OPENID;
  if (event._uid) return event._uid;
  return null;
};

// 按 username 查找用户（username 是唯一的权威标识）
const findUser = async (username) => {
  if (!username) return null;
  const q = await db.collection('users').where({ username }).get();
  return q.data.length ? q.data[0] : null;
};

// 按 username 获取用户的班级 ID 列表（权威来源：class_members 表）
const getUserClassIds = async (username) => {
  if (!username) return [];
  const memberQ = await db.collection('class_members').where({ username }).get();
  return memberQ.data.map(m => m.classId);
};

exports.main = async (event, context) => {
  const openid = getUserId(event);
  const action = event.action;
  const username = event.username;

  try {
    // 提交打卡
    if (action === 'submit') {
      const { exerciseId, exerciseName, exerciseIcon, met, duration, note } = event;
      if (!exerciseId || !duration || duration <= 0) {
        return { code: 1, message: '参数错误' };
      }
      const user = await findUser(username);
      if (!user) return { code: 1, message: '请先注册' };
      if (user.role !== 'student') return { code: 1, message: '仅学生可打卡' };

      const now = new Date();
      const calorie = calcCalorie(met, duration, user.weight || 30);
      
      const classIds = await getUserClassIds(username);

      const addRes = await db.collection('checkins').add({
        data: {
          username: user.username,
          userName: user.name,
          avatar: user.avatar,
          classIds,
          exerciseId,
          exerciseName,
          exerciseIcon,
          met,
          duration,
          calorie,
          note: note || '',
          image: event.image || '',
          createTime: now.getTime(),
          dateStr: formatDate(now),
          weekKey: getWeekKey(now),
          monthKey: getMonthKey(now)
        }
      });
      console.log('[submit] 打卡保存成功:', { _id: addRes._id, username, classIds });
      return { code: 0, data: { _id: addRes._id, calorie } };
    }

    // 今日打卡
    if (action === 'today') {
      const user = await findUser(username);
      if (!user) return { code: 0, data: [] };
      const start = new Date();
      start.setHours(0, 0, 0, 0);
      const end = new Date();
      end.setHours(23, 59, 59, 999);
      const q = await db.collection('checkins')
        .where({ username: user.username, createTime: _.gte(start.getTime()).and(_.lte(end.getTime())) })
        .orderBy('createTime', 'desc')
        .get();
      return { code: 0, data: q.data };
    }

    // 打卡历史（分页）
    if (action === 'list') {
      const page = event.page || 1;
      const pageSize = event.pageSize || 20;
      const targetUsername = event.targetUsername || username;
      
      const user = await findUser(username);
      if (!user) return { code: 1, message: '用户不存在' };
      
      // 若为家长请求孩子数据，需校验绑定关系
      if (targetUsername !== username) {
        const targetUser = await findUser(targetUsername);
        if (!targetUser || (user.role === 'parent' && targetUser.username !== user.childUsername)) {
          return { code: 1, message: '无权查看' };
        }
      }
      
      const total = (await db.collection('checkins').where({ username: targetUsername }).count).total;
      const q = await db.collection('checkins')
        .where({ username: targetUsername })
        .orderBy('createTime', 'desc')
        .skip((page - 1) * pageSize)
        .limit(pageSize)
        .get();
      return { code: 0, data: { list: q.data, total } };
    }

    // 获取图片临时链接
    if (action === 'getImageUrl') {
      const { fileID } = event;
      if (!fileID) {
        return { code: 1, message: '缺少 fileID' };
      }
      try {
        const result = await cloud.getTempFileURL({
          fileList: [fileID]
        });
        const url = result.fileList[0]?.tempFileURL;
        if (!url) {
          return { code: 1, message: '获取链接失败' };
        }
        return { code: 0, data: { url, fileID } };
      } catch (e) {
        console.error('getImageUrl error', e);
        return { code: 1, message: String(e.message || e) };
      }
    }

    // 班级打卡动态
    if (action === 'classFeed') {
      const { classId, page = 1, pageSize = 20 } = event;
      if (!classId) {
        return { code: 1, message: '缺少班级ID' };
      }
      const user = await findUser(username);
      if (!user) return { code: 1, message: '用户不存在' };
      
      // 验证用户是否属于该班级
      const userClassIds = await getUserClassIds(username);
      if (!userClassIds.includes(classId) && user.role !== 'teacher') {
        return { code: 1, message: '无权查看' };
      }
      
      // 获取班级所有成员的 username
      const memberQ = await db.collection('class_members').where({ classId }).get();
      const memberUsernames = memberQ.data.map(m => m.username).filter(Boolean);
      
      // 查询打卡记录：按成员 username 查询 + 按 classIds 查询，合并去重
      let allCheckins = [];
      const seen = new Set();
      
      // 方式1：按成员 username 查询
      if (memberUsernames.length > 0) {
        const q1 = await db.collection('checkins')
          .where({ username: _.in(memberUsernames) })
          .orderBy('createTime', 'desc')
          .limit(100)
          .get();
        q1.data.forEach(item => {
          if (!seen.has(item._id)) {
            seen.add(item._id);
            allCheckins.push(item);
          }
        });
      }
      
      // 方式2：按 classIds 查询（兼容历史数据）
      const q2 = await db.collection('checkins')
        .where({ classIds: _.in([classId]) })
        .orderBy('createTime', 'desc')
        .limit(100)
        .get();
      q2.data.forEach(item => {
        if (!seen.has(item._id)) {
          seen.add(item._id);
          allCheckins.push(item);
        }
      });
      
      allCheckins.sort((a, b) => (b.createTime || 0) - (a.createTime || 0));
      
      const total = allCheckins.length;
      const startIdx = (page - 1) * pageSize;
      const pagedList = allCheckins.slice(startIdx, startIdx + pageSize);
      
      console.log('[classFeed] 查询结果:', { total, memberUsernames });
      return { code: 0, data: { list: pagedList, total } };
    }

    // 删除打卡
    if (action === 'delete') {
      const { id } = event;
      const user = await findUser(username);
      if (!user) return { code: 1, message: '用户不存在' };
      const q = await db.collection('checkins').doc(id).get();
      if (!q.data || q.data.username !== user.username) {
        return { code: 1, message: '无权删除' };
      }
      await db.collection('checkins').doc(id).remove();
      return { code: 0, data: { ok: true } };
    }

    return { code: 1, message: '未知 action' };
  } catch (e) {
    console.error('checkin cloud function error', e);
    return { code: 1, message: String(e && e.message || e) };
  }
};

function getWeekKey(date) {
  const day = date.getDay() || 7;
  const monday = new Date(date);
  monday.setDate(date.getDate() - day + 1);
  return `W${monday.getFullYear()}${String(monday.getMonth() + 1).padStart(2, '0')}${String(monday.getDate()).padStart(2, '0')}`;
}

function getMonthKey(date) {
  return `M${date.getFullYear()}${String(date.getMonth() + 1).padStart(2, '0')}`;
}
