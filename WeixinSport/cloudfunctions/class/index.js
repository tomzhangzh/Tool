// cloudfunctions/class/index.js
const cloud = require('wx-server-sdk');
cloud.init({ env: cloud.DYNAMIC_CURRENT_ENV });
const db = cloud.database();
const _ = db.command;

const genCode = () => {
  const chars = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789';
  let s = '';
  for (let i = 0; i < 6; i++) s += chars[Math.floor(Math.random() * chars.length)];
  return s;
};

// 按 username 查找用户
const findUser = async (username) => {
  if (!username) return null;
  const q = await db.collection('users').where({ username }).get();
  return q.data.length ? q.data[0] : null;
};

// 判断某 username 是否为某班级的老师
const isClassTeacher = async (classId, username) => {
  if (!classId || !username) return false;
  const q = await db.collection('class_teachers').where({ classId, username }).count();
  return q.total > 0;
};

// 获取某 username 拥有管理权限的全部班级 ID
const getTeacherClassIds = async (username) => {
  const q = await db.collection('class_teachers').where({ username }).get();
  return q.data.map(t => t.classId);
};

exports.main = async (event, context) => {
  const action = event.action;
  const username = event.username;

  try {
    // 创建班级（老师）
    if (action === 'create') {
      const { name } = event;
      if (!name || !name.trim()) return { code: 1, message: '请填写班级名' };

      const user = await findUser(username);
      if (!user || user.role !== 'teacher') {
        return { code: 1, message: '仅老师可创建班级' };
      }

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
          creatorUsername: username,
          creatorName: user.name,
          status: 'active',
          createTime: now
        }
      });
      await db.collection('class_teachers').add({
        data: {
          classId: addRes._id,
          username,
          role: 'creator',
          joinTime: now
        }
      });
      const cls = (await db.collection('classes').doc(addRes._id).get()).data;
      return { code: 0, data: { ...cls, memberCount: 0, isTeacher: true } };
    }

    // 学生加入班级
    if (action === 'join') {
      const { code } = event;
      const clsQ = await db.collection('classes').where({ code, status: 'active' }).get();
      if (!clsQ.data.length) return { code: 1, message: '邀请码无效' };
      const cls = clsQ.data[0];

      const user = await findUser(username);
      if (!user) return { code: 1, message: '用户不存在，请先注册' };

      // 检查是否已加入
      const existQ = await db.collection('class_members').where({ classId: cls._id, username }).count();
      if (existQ.total > 0) return { code: 1, message: '已加入该班级' };

      await db.collection('class_members').add({
        data: {
          classId: cls._id,
          username: user.username,
          role: 'student',
          joinTime: Date.now()
        }
      });
      
      return { code: 0, data: { classId: cls._id } };
    }

    // 老师凭邀请码加入班级成为共管老师
    if (action === 'addTeacher') {
      const { code } = event;
      if (!code) return { code: 1, message: '请填写邀请码' };

      const user = await findUser(username);
      if (!user || user.role !== 'teacher') {
        return { code: 1, message: '仅老师可加入班级' };
      }

      const clsQ = await db.collection('classes').where({ code, status: 'active' }).get();
      if (!clsQ.data.length) return { code: 1, message: '邀请码无效' };
      const cls = clsQ.data[0];

      const exist = await db.collection('class_teachers').where({ classId: cls._id, username }).count();
      if (exist.total > 0) return { code: 1, message: '你已是该班级老师' };

      await db.collection('class_teachers').add({
        data: {
          classId: cls._id,
          username,
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
      const usernames = tQ.data.map(t => t.username).filter(Boolean);
      const users = usernames.length > 0 
        ? (await db.collection('users').where({ username: _.in(usernames) }).get()).data
        : [];
      const uMap = {};
      users.forEach(u => { uMap[u.username] = u; });
      const list = tQ.data
        .map(t => ({
          username: t.username,
          role: t.role,
          joinTime: t.joinTime,
          name: (uMap[t.username] && uMap[t.username].name) || '',
          avatar: (uMap[t.username] && uMap[t.username].avatar) || ''
        }))
        .sort((a, b) => (a.role === 'creator' ? -1 : 1) - (b.role === 'creator' ? -1 : 1));
      return { code: 0, data: list };
    }

    // 创建者移除共管老师（不能移除自己）
    if (action === 'removeTeacher') {
      const { classId, targetUsername } = event;
      if (!classId || !targetUsername) return { code: 1, message: '参数缺失' };
      if (targetUsername === username) return { code: 1, message: '不能移除自己' };

      const creatorQ = await db.collection('class_teachers')
        .where({ classId, username, role: 'creator' }).count();
      if (creatorQ.total === 0) return { code: 1, message: '仅创建者可移除老师' };

      await db.collection('class_teachers')
        .where({ classId, username: targetUsername, role: _.neq('creator') })
        .remove();
      return { code: 0, data: { ok: true } };
    }

    // 班级详情
    if (action === 'detail') {
      const { classId } = event;
      const cls = (await db.collection('classes').doc(classId).get()).data;
      const memberCount = (await db.collection('class_members').where({ classId }).count()).total;
      const teacherCount = (await db.collection('class_teachers').where({ classId }).count()).total;
      const isTeacher = await isClassTeacher(classId, username);
      return { code: 0, data: { ...cls, memberCount, teacherCount, isTeacher } };
    }

    // 班级成员列表
    if (action === 'members') {
      const { classId } = event;
      const memberQ = await db.collection('class_members').where({ classId, role: 'student' }).get();
      const usernames = memberQ.data.map(m => m.username).filter(Boolean);
      if (!usernames.length) return { code: 0, data: [] };

      const users = await db.collection('users').where({ username: _.in(usernames) }).get();
      const userMap = {};
      users.data.forEach(u => { userMap[u.username] = u; });

      // 取本周卡路里
      const now = new Date();
      const day = now.getDay() || 7;
      const monday = new Date(now);
      monday.setDate(now.getDate() - day + 1);
      monday.setHours(0, 0, 0, 0);

      const checkinQ = await db.collection('checkins')
        .where({ username: _.in(usernames), createTime: _.gte(monday.getTime()) })
        .get();
      const calMap = {};
      const cntMap = {};
      checkinQ.data.forEach(c => {
        calMap[c.username] = (calMap[c.username] || 0) + (c.calorie || 0);
        cntMap[c.username] = (cntMap[c.username] || 0) + 1;
      });

      const list = memberQ.data.map(m => {
        const u = userMap[m.username] || {};
        return {
          _id: u._id || m._id,
          username: m.username,
          name: u.name || '',
          avatar: u.avatar || '',
          totalCalorie: calMap[m.username] || 0,
          totalCheckins: cntMap[m.username] || 0
        };
      }).sort((a, b) => b.totalCalorie - a.totalCalorie);

      return { code: 0, data: list };
    }

    // 我的班级列表
    if (action === 'my') {
      const user = await findUser(username);
      if (!user) return { code: 0, data: [] };

      // 老师：通过 class_teachers 关联表
      if (user.role === 'teacher') {
        let tIds = await getTeacherClassIds(username);

        // 老数据兼容：把以本老师为创建者、但 class_teachers 缺失的班级回填
        const legacyQ = await db.collection('classes').where({ creatorUsername: username }).get();
        const legacyMissing = legacyQ.data.filter(c => !tIds.includes(c._id));
        if (legacyMissing.length) {
          const now = Date.now();
          for (const c of legacyMissing) {
            await db.collection('class_teachers').add({
              data: { classId: c._id, username, role: 'creator', joinTime: now }
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

      // 学生：通过 class_members 表
      let classIds = [];
      if (user.role === 'student') {
        const q = await db.collection('class_members').where({ username }).get();
        classIds = q.data.map(m => m.classId);
      }

      // 家长：看孩子班级
      if (user.role === 'parent' && user.childUsername) {
        const q = await db.collection('class_members').where({ username: user.childUsername }).get();
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

    // 退出班级（学生或老师均可）
    if (action === 'quitClass') {
      const { classId } = event;
      if (!classId) return { code: 1, message: '请选择要退出的班级' };

      const user = await findUser(username);
      if (!user) return { code: 1, message: '用户不存在' };

      const clsQ = await db.collection('classes').where({ _id: classId }).get();
      if (!clsQ.data.length) return { code: 1, message: '班级不存在' };
      const cls = clsQ.data[0];

      // 检查是否为班级创建者
      const creatorQ = await db.collection('class_teachers').where({ classId, role: 'creator', username }).get();
      if (creatorQ.data.length > 0) {
        return { code: 1, message: '您是该班级的创建者，无法退出。请先解散班级或转让创建者权限。' };
      }

      if (user.role === 'teacher') {
        await db.collection('class_teachers').where({ classId, username }).remove();
      } else {
        await db.collection('class_members').where({ classId, username }).remove();
      }

      return { code: 0, data: { ok: true, className: cls.name } };
    }

    // 家长绑定孩子
    if (action === 'bindChild') {
      const { childUsername } = event;
      const user = await findUser(username);
      if (!user) return { code: 1, message: '用户不存在' };
      
      await db.collection('users').doc(user._id).update({
        data: { childUsername }
      });
      
      // 同步孩子的 parentUsernames
      const child = await findUser(childUsername);
      if (child) {
        const parents = child.parentUsernames || [];
        if (!parents.includes(username)) {
          await db.collection('users').doc(child._id).update({
            data: { parentUsernames: _.push([username]) }
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
