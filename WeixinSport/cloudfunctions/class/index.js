// cloudfunctions/class/index.js
const cloud = require('wx-server-sdk');
cloud.init({ env: cloud.DYNAMIC_CURRENT_ENV });
const db = cloud.database();
const _ = db.command;

// 生成6位邀请码
const genCode = () => {
  const chars = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789';
  let s = '';
  for (let i = 0; i < 6; i++) s += chars[Math.floor(Math.random() * chars.length)];
  return s;
};

exports.main = async (event, context) => {
  const wxCtx = cloud.getWXContext();
  const openid = wxCtx.OPENID;
  const action = event.action;

  try {
    // 创建班级（老师）
    if (action === 'create') {
      const { name } = event;
      if (!name || !name.trim()) return { code: 1, message: '请填写班级名' };

      // 确认是老师
      const userQ = await db.collection('users').where({ openid }).get();
      if (!userQ.data.length || userQ.data[0].role !== 'teacher') {
        return { code: 1, message: '仅老师可创建班级' };
      }

      // 生成不重复邀请码
      let code = genCode();
      for (let i = 0; i < 5; i++) {
        const exist = await db.collection('classes').where({ code }).count();
        if (exist.total === 0) break;
        code = genCode();
      }

      const now = Date.now();
      const addRes = await db.collection('classes').add({
        data: {
          name: name.trim(),
          code,
          teacherOpenid: openid,
          teacherName: userQ.data[0].name,
          status: 'active',
          createTime: now
        }
      });
      const cls = (await db.collection('classes').doc(addRes._id).get()).data;
      return { code: 0, data: { ...cls, memberCount: 0 } };
    }

    // 学生加入班级
    if (action === 'join') {
      const { code } = event;
      const clsQ = await db.collection('classes').where({ code, status: 'active' }).get();
      if (!clsQ.data.length) return { code: 1, message: '邀请码无效' };
      const cls = clsQ.data[0];

      // 已加入？
      const memberQ = await db.collection('class_members').where({ classId: cls._id, openid }).get();
      if (memberQ.data.length) return { code: 1, message: '已加入该班级' };

      await db.collection('class_members').add({
        data: {
          classId: cls._id,
          openid,
          role: 'student',
          joinTime: Date.now()
        }
      });
      // 写入用户 classIds
      const userQ = await db.collection('users').where({ openid }).get();
      if (userQ.data.length) {
        await db.collection('users').doc(userQ.data[0]._id).update({
          data: { classIds: _.push([cls._id]) }
        });
      }
      return { code: 0, data: { classId: cls._id } };
    }

    // 班级详情
    if (action === 'detail') {
      const { classId } = event;
      const cls = (await db.collection('classes').doc(classId).get()).data;
      const memberCount = (await db.collection('class_members').where({ classId }).count()).total;
      return { code: 0, data: { ...cls, memberCount } };
    }

    // 班级成员列表
    if (action === 'members') {
      const { classId } = event;
      const memberQ = await db.collection('class_members').where({ classId, role: 'student' }).get();
      const openids = memberQ.data.map(m => m.openid);
      if (!openids.length) return { code: 0, data: [] };

      const users = await db.collection('users').where({ openid: _.in(openids) }).get();
      // 取本周卡路里
      const now = new Date();
      const day = now.getDay() || 7;
      const monday = new Date(now);
      monday.setDate(now.getDate() - day + 1);
      monday.setHours(0, 0, 0, 0);

      const checkinQ = await db.collection('checkins')
        .where({ openid: _.in(openids), createTime: _.gte(monday.getTime()) })
        .get();
      const calMap = {};
      const cntMap = {};
      checkinQ.data.forEach(c => {
        calMap[c.openid] = (calMap[c.openid] || 0) + (c.calorie || 0);
        cntMap[c.openid] = (cntMap[c.openid] || 0) + 1;
      });

      const list = users.data.map(u => ({
        _id: u._id,
        openid: u.openid,
        name: u.name,
        avatar: u.avatar,
        totalCalorie: calMap[u.openid] || 0,
        totalCheckins: cntMap[u.openid] || 0
      })).sort((a, b) => b.totalCalorie - a.totalCalorie);

      return { code: 0, data: list };
    }

    // 我的班级列表
    if (action === 'my') {
      const userQ = await db.collection('users').where({ openid }).get();
      const user = userQ.data[0];
      if (!user) return { code: 0, data: [] };

      // 老师：作为创建者
      // 学生/家长：通过 class_members 或孩子的 class_members
      let classIds = [];
      if (user.role === 'teacher') {
        const q = await db.collection('classes').where({ teacherOpenid: openid }).get();
        const list = [];
        for (const c of q.data) {
          const memberCount = (await db.collection('class_members').where({ classId: c._id }).count()).total;
          list.push({ ...c, memberCount });
        }
        return { code: 0, data: list };
      }

      // 学生
      if (user.role === 'student') {
        const q = await db.collection('class_members').where({ openid }).get();
        classIds = q.data.map(m => m.classId);
      }

      // 家长：看孩子班级
      if (user.role === 'parent' && user.childOpenid) {
        const q = await db.collection('class_members').where({ openid: user.childOpenid }).get();
        classIds = q.data.map(m => m.classId);
      }

      if (!classIds.length) return { code: 0, data: [] };
      const clsQ = await db.collection('classes').where({ _id: _.in(classIds) }).get();
      const list = [];
      for (const c of clsQ.data) {
        const memberCount = (await db.collection('class_members').where({ classId: c._id }).count()).total;
        list.push({ ...c, memberCount });
      }
      return { code: 0, data: list };
    }

    // 家长绑定孩子（也可由 login 调用 bindRole 完成首次绑定，此处用于切换）
    if (action === 'bindChild') {
      const { childOpenid } = event;
      const userQ = await db.collection('users').where({ openid }).get();
      if (!userQ.data.length) return { code: 1, message: '用户不存在' };
      await db.collection('users').doc(userQ.data[0]._id).update({
        data: { childOpenid }
      });
      // 同步孩子的 parentOpenids
      const childQ = await db.collection('users').where({ openid: childOpenid }).get();
      if (childQ.data.length) {
        const child = childQ.data[0];
        const parents = child.parentOpenids || [];
        if (!parents.includes(openid)) {
          await db.collection('users').doc(child._id).update({
            data: { parentOpenids: _.push([openid]) }
          });
        }
      }
      return { code: 0, data: { ok: true } };
    }

    return { code: 1, message: '未知 action' };
  } catch (e) {
    console.error('class cloud function error', e);
    return { code: 1, message: String(e && e.message || e) };
  }
};
