// cloudfunctions/login/index.js
const cloud = require('wx-server-sdk');
cloud.init({ env: cloud.DYNAMIC_CURRENT_ENV });
const db = cloud.database();
const _ = db.command;

// 获取用户 ID：兼容小程序（OPENID）和 H5（event._uid）环境
const getUserId = (event) => {
  const wxCtx = cloud.getWXContext();
  // 优先使用小程序的 OPENID
  if (wxCtx.OPENID) {
    return { userId: wxCtx.OPENID, userIdType: 'openid' };
  }
  // H5 环境使用前端传入的 _uid
  if (event._uid) {
    return { userId: event._uid, userIdType: 'uid' };
  }
  return { userId: null, userIdType: null };
};

// 登录 / 绑定角色 / 获取个人信息 / 更新个人信息
exports.main = async (event, context) => {
  const { userId: openid, userIdType } = getUserId(event);
  const action = event.action || 'login';

  if (!openid) {
    return { code: 1, message: '未获取到用户身份' };
  }

  try {
    if (action === 'login') {
      // 只查询返回 openid + 已有用户信息；不在此处创建用户，
      // 避免与 bindRole 的 upsert 叠加产生重复记录。
      const userCol = db.collection('users');
      const existing = await userCol.where({ openid }).orderBy('createTime', 'asc').get();
      const user = existing.data[0];
      return { code: 0, data: { openid, ...(user || {}) } };
    }

    if (action === 'bindRole') {
      // 注册/绑定角色，由 login 页调用
      const { role, name, avatar, weight, classCode, childName } = event;
      const update = {
        role,
        name,
        avatar: avatar || '',
        updateTime: Date.now()
      };
      if (role === 'student') {
        update.weight = weight || 30;
      }

      // 学生加入班级
      let joinedClassIds = [];
      if (role === 'student' && classCode) {
        const classCol = db.collection('classes');
        const cls = await classCol.where({ code: classCode, status: 'active' }).get();
        if (!cls.data.length) {
          return { code: 1, message: '邀请码无效或班级已关闭' };
        }
        const c = cls.data[0];
        joinedClassIds = [c._id];
        update.classIds = joinedClassIds;
        // 班级成员表登记
        await db.collection('class_members').add({
          data: {
            classId: c._id,
            openid,
            role: 'student',
            joinTime: Date.now()
          }
        });
      }

      // 家长绑定孩子
      if (role === 'parent' && childName) {
        // 根据姓名找孩子（同名可能冲突，需老师人工核对）
        const stu = await db.collection('users').where({ role: 'student', name: childName }).get();
        if (!stu.data.length) {
          return { code: 1, message: '未找到该姓名的学生，请确认孩子已注册' };
        }
        // 取第一个匹配（实际可加班级筛选）
        const child = stu.data[0];
        update.childOpenid = child.openid;
        // 把家长 openid 加入孩子的 parentOpenids
        await db.collection('users').doc(child._id).update({
          data: { parentOpenids: _.push([openid]) }
        });
      }

      // 写入更新（按 createTime 升序，保证同 openid 多条记录时稳定取最早一条）
      const userCol = db.collection('users');
      const existing = await userCol.where({ openid }).orderBy('createTime', 'asc').get();

      // 去重：若历史遗留导致同 openid 多条记录，保留最早一条并合并缺失字段，删除其余
      if (existing.data.length > 1) {
        const keep = existing.data[0];
        const mergeFields = {};
        existing.data.slice(1).forEach(other => {
          Object.keys(other).forEach(k => {
            if (k === '_id' || k === '_openid') return;
            if (keep[k] === undefined && other[k] !== undefined && other[k] !== '' && !(Array.isArray(other[k]) && other[k].length === 0)) {
              mergeFields[k] = other[k];
            }
          });
        });
        // 删除多余记录
        for (let i = 1; i < existing.data.length; i++) {
          await userCol.doc(existing.data[i]._id).remove();
        }
        await userCol.doc(keep._id).update({ data: { ...mergeFields } });
        existing.data = [keep];
      }

      let userId;
      if (existing.data.length) {
        await userCol.doc(existing.data[0]._id).update({ data: update });
        userId = existing.data[0]._id;
      } else {
        const addRes = await userCol.add({
          data: {
            openid,
            role,
            name,
            avatar: avatar || '',
            weight: role === 'student' ? (weight || 30) : 30,
            classIds: joinedClassIds,
            childOpenid: '',
            parentOpenids: [],
            createTime: Date.now(),
            updateTime: Date.now()
          }
        });
        userId = addRes._id;
      }
      const finalUser = (await userCol.doc(userId).get()).data;
      return { code: 0, data: finalUser };
    }

    if (action === 'getProfile') {
      // 个人中心：返回汇总统计 + 奖项数
      const userQ = await db.collection('users').where({ openid }).orderBy('createTime', 'asc').get();
      if (!userQ.data.length) return { code: 1, message: '用户不存在' };
      const user = userQ.data[0];

      // 视角：家长看孩子
      let targetOpenid = openid;
      if (user.role === 'parent' && user.childOpenid) {
        targetOpenid = user.childOpenid;
      }

      // 汇总打卡数据
      const agg = db.collection('checkins').aggregate();
      const statRes = await agg.match({ openid: targetOpenid })
        .group({
          _id: null,
          totalDuration: { $sum: '$duration' },
          totalCalorie: { $sum: '$calorie' },
          totalCheckins: { $sum: 1 }
        })
        .end();
      const summary = statRes.list[0] || { totalDuration: 0, totalCalorie: 0, totalCheckins: 0 };

      // 坚持天数（去重打卡日）
      const dayAgg = db.collection('checkins').aggregate();
      const dayRes = await dayAgg.match({ openid: targetOpenid })
        .group({ _id: '$dateStr' })
        .count('days')
        .end();
      summary.totalDays = dayRes.list.length || 0;

      // 奖项数
      const awardCount = (await db.collection('awards').where({ openid: targetOpenid }).count()).total;

      return { code: 0, data: { summary, awardCount } };
    }

    if (action === 'updateProfile') {
      const update = { ...event };
      delete update.action;
      update.updateTime = Date.now();
      const userQ = await db.collection('users').where({ openid }).orderBy('createTime', 'asc').get();
      if (userQ.data.length) {
        await db.collection('users').doc(userQ.data[0]._id).update({ data: update });
      }
      const updated = (await db.collection('users').doc(userQ.data[0]._id).get()).data;
      return { code: 0, data: updated };
    }

    // 一次性全表去重：合并同 openid 的多余记录，保留 createTime 最早一条
    if (action === 'dedupUsers') {
      const LIMIT = 100;
      let removed = 0;
      let processed = 0;
      // 简化策略：遍历所有用户，按 openid 分组，对存在多条的清理多余记录
      // 由于云函数遍历有上限，这里分批拉取
      const all = await db.collection('users').orderBy('createTime', 'asc').limit(LIMIT).get();
      processed = all.data.length;
      const byOpenid = {};
      all.data.forEach(u => {
        if (!u.openid) return;
        (byOpenid[u.openid] = byOpenid[u.openid] || []).push(u);
      });
      for (const ids of Object.values(byOpenid)) {
        if (ids.length < 2) continue;
        const keep = ids[0];
        const mergeFields = {};
        ids.slice(1).forEach(other => {
          Object.keys(other).forEach(k => {
            if (k === '_id' || k === '_openid') return;
            if (keep[k] === undefined && other[k] !== undefined && other[k] !== '' && !(Array.isArray(other[k]) && other[k].length === 0)) {
              mergeFields[k] = other[k];
            }
          });
        });
        // 删除多余记录
        for (let i = 1; i < ids.length; i++) {
          await db.collection('users').doc(ids[i]._id).remove();
          removed++;
        }
        // 合并缺失字段到保留记录
        if (Object.keys(mergeFields).length) {
          await db.collection('users').doc(keep._id).update({ data: mergeFields });
        }
      }
      return { code: 0, data: { processed, removed } };
    }

    return { code: 1, message: '未知 action' };
  } catch (e) {
    console.error('login cloud function error', e);
    return { code: 1, message: String(e && e.message || e) };
  }
};
