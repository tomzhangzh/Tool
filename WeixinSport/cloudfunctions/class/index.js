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

// 判断某 openid 是否为某班级的老师（creator 或 teacher）
const isClassTeacher = async (classId, openid) => {
  if (!classId || !openid) return false;
  const q = await db.collection('class_teachers').where({ classId, openid }).count();
  return q.total > 0;
};

// 获取某 openid 拥有管理权限的全部班级 ID
const getTeacherClassIds = async (openid) => {
  const q = await db.collection('class_teachers').where({ openid }).get();
  return q.data.map(t => t.classId);
};

// 获取用户 ID：兼容小程序（OPENID）和 H5（event._uid）环境
const getUserId = (event) => {
  const wxCtx = cloud.getWXContext();
  if (wxCtx.OPENID) return wxCtx.OPENID;
  if (event._uid) return event._uid;
  return null;
};

exports.main = async (event, context) => {
  const openid = getUserId(event);
  if (!openid) return { code: 1, message: '未获取到用户身份' };
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
          teacherOpenid: openid,        // 创建者（保留用于展示）
          teacherName: userQ.data[0].name,
          status: 'active',
          createTime: now
        }
      });
      // 同步写入 class_teachers 关联表
      await db.collection('class_teachers').add({
        data: {
          classId: addRes._id,
          openid,
          role: 'creator',
          joinTime: now
        }
      });
      const cls = (await db.collection('classes').doc(addRes._id).get()).data;
      return { code: 0, data: { ...cls, memberCount: 0, isTeacher: true } };
    }

    // 学生加入班级
    if (action === 'join') {
      const { code, username } = event;
      const clsQ = await db.collection('classes').where({ code, status: 'active' }).get();
      if (!clsQ.data.length) return { code: 1, message: '邀请码无效' };
      const cls = clsQ.data[0];

      // 已加入？（容错：检查当前 openid 和可能的旧 openid）
      let memberQ = await db.collection('class_members').where({ classId: cls._id, openid }).get();
      if (memberQ.data.length) return { code: 1, message: '已加入该班级' };
      
      // 容错：查找用户（可能 openid 已变化）
      let userQ = await db.collection('users').where({ openid }).get();
      let user = userQ.data[0];
      if (!user && username) {
        userQ = await db.collection('users').where({ username }).get();
        user = userQ.data[0];
        // 如果找到用户但 openid 不匹配，更新 openid
        if (user && user.openid !== openid) {
          await db.collection('users').doc(user._id).update({
            data: { openid, updateTime: Date.now() }
          });
          console.log('[join] 更新用户 openid:', { old: user.openid, new: openid });
        }
      }
      if (!user) return { code: 1, message: '用户不存在，请先注册' };
      
      // 检查用户是否已加入（用用户的当前 openid 或旧 openid）
      memberQ = await db.collection('class_members').where({ classId: cls._id, openid: user.openid }).get();
      if (memberQ.data.length) return { code: 1, message: '已加入该班级' };

      // 添加成员记录
      await db.collection('class_members').add({
        data: {
          classId: cls._id,
          openid: user.openid,
          role: 'student',
          joinTime: Date.now()
        }
      });
      
      // 写入用户 classIds
      const currentClassIds = user.classIds || [];
      if (!currentClassIds.includes(cls._id)) {
        await db.collection('users').doc(user._id).update({
          data: { classIds: _.push([cls._id]) }
        });
        console.log('[join] 更新用户 classIds:', { classIds: [...currentClassIds, cls._id] });
      }
      
      return { code: 0, data: { classId: cls._id } };
    }

    // 老师凭邀请码加入班级成为共管老师
    if (action === 'addTeacher') {
      const { code } = event;
      if (!code) return { code: 1, message: '请填写邀请码' };

      const userQ = await db.collection('users').where({ openid }).get();
      if (!userQ.data.length || userQ.data[0].role !== 'teacher') {
        return { code: 1, message: '仅老师可加入班级' };
      }

      const clsQ = await db.collection('classes').where({ code, status: 'active' }).get();
      if (!clsQ.data.length) return { code: 1, message: '邀请码无效' };
      const cls = clsQ.data[0];

      // 已经是该班级老师？
      const exist = await db.collection('class_teachers').where({ classId: cls._id, openid }).count();
      if (exist.total > 0) return { code: 1, message: '你已是该班级老师' };

      await db.collection('class_teachers').add({
        data: {
          classId: cls._id,
          openid,
          role: 'teacher',
          joinTime: Date.now()
        }
      });
      return { code: 0, data: { classId: cls._id, name: cls.name } };
    }

    // 班级老师列表
    if (action === 'listTeachers') {
      const { classId } = event;
      const tQ = await db.collection('class_teachers').where({ classId }).get();
      if (!tQ.data.length) return { code: 0, data: [] };
      const openids = tQ.data.map(t => t.openid);
      const users = await db.collection('users').where({ openid: _.in(openids) }).get();
      const uMap = {};
      users.data.forEach(u => { uMap[u.openid] = u; });
      const list = tQ.data
        .map(t => ({
          openid: t.openid,
          role: t.role,
          joinTime: t.joinTime,
          name: (uMap[t.openid] && uMap[t.openid].name) || '',
          avatar: (uMap[t.openid] && uMap[t.openid].avatar) || ''
        }))
        .sort((a, b) => (a.role === 'creator' ? -1 : 1) - (b.role === 'creator' ? -1 : 1));
      return { code: 0, data: list };
    }

    // 创建者移除共管老师（不能移除自己）
    if (action === 'removeTeacher') {
      const { classId, targetOpenid } = event;
      if (!classId || !targetOpenid) return { code: 1, message: '参数缺失' };
      if (targetOpenid === openid) return { code: 1, message: '不能移除自己' };

      // 当前用户必须是该班级创建者
      const creatorQ = await db.collection('class_teachers')
        .where({ classId, openid, role: 'creator' }).count();
      if (creatorQ.total === 0) return { code: 1, message: '仅创建者可移除老师' };

      await db.collection('class_teachers')
        .where({ classId, openid: targetOpenid, role: _.neq('creator') })
        .remove();
      return { code: 0, data: { ok: true } };
    }

    // 班级详情
    if (action === 'detail') {
      const { classId } = event;
      const cls = (await db.collection('classes').doc(classId).get()).data;
      const memberCount = (await db.collection('class_members').where({ classId }).count()).total;
      const teacherCount = (await db.collection('class_teachers').where({ classId }).count()).total;
      const isTeacher = await isClassTeacher(classId, openid);
      return { code: 0, data: { ...cls, memberCount, teacherCount, isTeacher } };
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
      // 容错查找用户：先按 openid，找不到按 username
      let userQ = await db.collection('users').where({ openid }).get();
      let user = userQ.data[0];
      if (!user && event.username) {
        userQ = await db.collection('users').where({ username: event.username }).get();
        user = userQ.data[0];
        // 如果找到用户但 openid 不匹配，更新 openid
        if (user && user.openid !== openid) {
          await db.collection('users').doc(user._id).update({
            data: { openid, updateTime: Date.now() }
          });
          console.log('[my] 更新用户 openid:', { old: user.openid, new: openid });
          user.openid = openid;
        }
      }
      if (!user) return { code: 0, data: [] };

      // 老师：通过 class_teachers 关联表
      if (user.role === 'teacher') {
        let tIds = await getTeacherClassIds(openid);

        // 老数据兼容：把以本老师为创建者、但 class_teachers 缺失的班级回填
        const legacyQ = await db.collection('classes').where({ teacherOpenid: openid }).get();
        const legacyMissing = legacyQ.data.filter(c => !tIds.includes(c._id));
        if (legacyMissing.length) {
          const now = Date.now();
          for (const c of legacyMissing) {
            await db.collection('class_teachers').add({
              data: { classId: c._id, openid, role: 'creator', joinTime: now }
            });
            tIds.push(c._id);
          }
        }

        if (!tIds.length) return { code: 0, data: [] };
        const clsQ = await db.collection('classes').where({ _id: _.in(tIds) }).get();
        const list = [];
        for (const c of clsQ.data) {
          const memberCount = (await db.collection('class_members').where({ classId: c._id }).count()).total;
          const teacherCount = (await db.collection('class_teachers').where({ classId: c._id }).count()).total;
          list.push({ ...c, memberCount, teacherCount, isTeacher: true });
        }
        return { code: 0, data: list };
      }

      // 学生
      let classIds = [];
      if (user.role === 'student') {
        // 先按当前 openid 查
        let q = await db.collection('class_members').where({ openid }).get();
        classIds = q.data.map(m => m.classId);
        
        // 容错：如果没查到，尝试用旧 openid（用户原来的 openid）查
        if (classIds.length === 0 && user.openid !== openid) {
          q = await db.collection('class_members').where({ openid: user.openid }).get();
          classIds = q.data.map(m => m.classId);
          // 如果查到了，更新 class_members 中的 openid
          if (classIds.length > 0) {
            const now = Date.now();
            for (const m of q.data) {
              if (m.openid !== openid) {
                await db.collection('class_members').doc(m._id).update({
                  data: { openid, updateTime: now }
                });
              }
            }
            console.log('[my] 更新 class_members openid:', { classIds });
          }
        }
        
        // 再容错：如果还没查到，从用户的 classIds 字段获取
        if (classIds.length === 0 && user.classIds && user.classIds.length) {
          classIds = user.classIds;
          console.log('[my] 从用户 classIds 字段获取:', { classIds });
        }
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
        const teacherCount = (await db.collection('class_teachers').where({ classId: c._id }).count()).total;
        list.push({ ...c, memberCount, teacherCount, isTeacher: false });
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
