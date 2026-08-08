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

// 根据 openid 或 username 查找用户（容错 H5 环境 uid 变化）
const findUser = async (openid, username) => {
  const userCol = db.collection('users');
  
  // 1. 优先用 openid 查找
  if (openid) {
    const q1 = await userCol.where({ openid }).get();
    if (q1.data.length) return q1.data[0];
  }
  
  // 2. 容错：用 username 查找（账号登录场景）
  if (username) {
    const q2 = await userCol.where({ username }).get();
    if (q2.data.length) {
      const user = q2.data[0];
      // 如果找到用户但 openid 不匹配，更新 openid
      if (openid && user.openid !== openid) {
        await userCol.doc(user._id).update({
          data: { openid, updateTime: Date.now() }
        });
        console.log('[findUser] 更新用户 openid:', { old: user.openid, new: openid });
        user.openid = openid;
      }
      return user;
    }
  }
  
  return null;
};

exports.main = async (event, context) => {
  const openid = getUserId(event);
  if (!openid) return { code: 1, message: '未获取到用户身份' };
  const action = event.action;

  try {
    // 提交打卡
    if (action === 'submit') {
      const { exerciseId, exerciseName, exerciseIcon, met, duration, note, username } = event;
      if (!exerciseId || !duration || duration <= 0) {
        return { code: 1, message: '参数错误' };
      }
      // 查找用户（兼容 openid 变化场景）
      const user = await findUser(openid, username);
      if (!user) return { code: 1, message: '请先注册' };
      if (user.role !== 'student') return { code: 1, message: '仅学生可打卡' };

      const now = new Date();
      const calorie = calcCalorie(met, duration, user.weight || 30);
      
      // 获取 classIds：优先从用户字段，如果为空则从 class_members 查询
      let classIds = user.classIds || [];
      if (classIds.length === 0 && username) {
        const memberQ = await db.collection('class_members').where({ openid: user.openid }).get();
        classIds = memberQ.data.map(m => m.classId);
        // 如果查到了，更新用户的 classIds
        if (classIds.length > 0) {
          await db.collection('users').doc(user._id).update({
            data: { classIds, updateTime: Date.now() }
          });
          console.log('[submit] 从 class_members 恢复 classIds:', { classIds });
        }
      }

      const addRes = await db.collection('checkins').add({
        data: {
          openid: user.openid,
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
          // 周/月标识便于后续聚合
          weekKey: getWeekKey(now),
          monthKey: getMonthKey(now)
        }
      });
      console.log('[submit] 打卡保存成功:', { _id: addRes._id, image: event.image, classIds });
      return { code: 0, data: { _id: addRes._id, calorie } };
    }

    // 今日打卡
    if (action === 'today') {
      const start = new Date();
      start.setHours(0, 0, 0, 0);
      const end = new Date();
      end.setHours(23, 59, 59, 999);
      const q = await db.collection('checkins')
        .where({ openid, createTime: _.gte(start.getTime()).and(_.lte(end.getTime())) })
        .orderBy('createTime', 'desc')
        .get();
      return { code: 0, data: q.data };
    }

    // 打卡历史（分页）
    if (action === 'list') {
      const page = event.page || 1;
      const pageSize = event.pageSize || 20;
      const targetOpenid = event.targetOpenid || openid; // 家长看孩子时传孩子 openid
      // 若为家长请求孩子数据，需校验绑定关系
      if (targetOpenid !== openid) {
        const user = await findUser(openid, event.username);
        if (!user || (user.role === 'parent' && user.childOpenid !== targetOpenid)) {
          return { code: 1, message: '无权查看' };
        }
      }
      const total = (await db.collection('checkins').where({ openid: targetOpenid }).count).total;
      const q = await db.collection('checkins')
        .where({ openid: targetOpenid })
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
      // 验证用户是否属于该班级
      const user = await findUser(openid, event.username);
      if (!user) return { code: 1, message: '用户不存在' };
      
      // 容错：检查用户是否属于该班级
      let userClassIds = user.classIds || [];
      if (!userClassIds.includes(classId) && user.role !== 'teacher') {
        // 从 class_members 表检查
        const memberQ = await db.collection('class_members').where({ openid: user.openid, classId }).get();
        if (memberQ.data.length > 0) {
          userClassIds.push(classId);
          // 更新用户 classIds
          await db.collection('users').doc(user._id).update({
            data: { classIds: _.push([classId]), updateTime: Date.now() }
          });
          console.log('[classFeed] 从 class_members 恢复 classId:', { classId });
        } else {
          return { code: 1, message: '无权查看' };
        }
      }
      
      // 获取班级所有成员的 openid（用于查询历史数据）
      const memberQ = await db.collection('class_members').where({ classId }).get();
      const memberOpenids = memberQ.data.map(m => m.openid);
      
      // 查询打卡记录：两种方式都查，合并去重
      // 1. 按 classIds 字段查（新数据）
      // 2. 按成员 openid 查（历史数据，classIds 可能为空）
      let allCheckins = [];
      
      if (memberOpenids.length > 0) {
        const q1 = await db.collection('checkins')
          .where({ openid: _.in(memberOpenids) })
          .orderBy('createTime', 'desc')
          .limit(100)
          .get();
        allCheckins = q1.data;
      } else {
        // 如果没有成员，用 classIds 方式查
        const q2 = await db.collection('checkins')
          .where({ classIds: _.in([classId]) })
          .orderBy('createTime', 'desc')
          .limit(100)
          .get();
        allCheckins = q2.data;
      }
      
      // 去重（按 _id）
      const seen = new Set();
      const uniqueList = allCheckins.filter(item => {
        if (seen.has(item._id)) return false;
        seen.add(item._id);
        return true;
      });
      
      // 分页
      const total = uniqueList.length;
      const startIdx = (page - 1) * pageSize;
      const pagedList = uniqueList.slice(startIdx, startIdx + pageSize);
      
      console.log('[classFeed] 查询结果:', { total, memberOpenids });
      return { code: 0, data: { list: pagedList, total } };
    }

    // 删除打卡
    if (action === 'delete') {
      const { id } = event;
      const q = await db.collection('checkins').doc(id).get();
      if (!q.data || q.data.openid !== openid) {
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

// 周标识：W20250105 (本周一日期)
function getWeekKey(date) {
  const day = date.getDay() || 7;
  const monday = new Date(date);
  monday.setDate(date.getDate() - day + 1);
  return `W${monday.getFullYear()}${String(monday.getMonth() + 1).padStart(2, '0')}${String(monday.getDate()).padStart(2, '0')}`;
}

function getMonthKey(date) {
  return `M${date.getFullYear()}${String(date.getMonth() + 1).padStart(2, '0')}`;
}
