/*
 * This is the constant object where we define some constants for the application.
 * APP_NAME: The name of the application.
 * EL_EVENT_PREFIX: The prefix for element events.
 * TARGET_EVENT_PREFIX: The prefix for target events.
 */

const CONST = {
    APP_NAME: 'tAPP', // 应用名称
    EL_EVENT_PREFIX: 'el', // 元素事件前缀
    TARGET_EVENT_PREFIX: 'target', // 目标事件前缀
};
var nameSpace = {
    register: function (namespace, obj) { // 注册命名空间
        var parts = namespace.split('.');
        var parent = window;
        for (var i = 0; i < parts.length; i++) {
            if (!parent[parts[i]]) {
                parent[parts[i]] = {};
            }
            parent = parent[parts[i]];
        }
        if (obj) {
            Object.assign(parent, obj);
        }
    }
};
// 注册命名空间函数，用于创建或获取全局命名空间对象，并将传入的对象合并到该命名空间
// 异步函数构造器，用于创建异步函数
// 实用工具集，提供了一系列常用的工具方法，如生成GUID、新ID、新名称、HTML编码解码、获取公共DIV、判断函数是否异步、执行字符串代码、JSON对象转字符串等


// Shim for allowing async function creation via new Function
// This is a shim to allow the creation of async functions using the new Function syntax


AsyncFunction = Object.getPrototypeOf(async function () { }).constructor;
var utility = {
    guid: () => { // 生成guid
        function s4() {
            return Math.floor((1 + Math.random()) * 0x10000)
                .toString(16)
                .substring(1);
        }
        return s4() + s4() + '-' + s4() + '-' + s4() + '-' +
            s4() + '-' + s4() + s4() + s4();
    },
    newId: () => { // 生成新的id
        return `Ctrl${utility.guid()}`;
    },
    newName: () => { // 生成新的名称
        return `Name${utility.guid()}`;
    },
    htmlEncode: function (value) { // html编码
        return $('<div/>').text(value).html();
    },
    htmlDecode: function (value) { // html解码
        return $('<div/>').html(value).text();
    },
    publicDIV: function () { // 获取公共div
        var $div = $('#PublicDiv');
        if ($div.length === 0) {
            $div = $('<div id="PublicDiv" style="display:none"></div>').appendTo('body');
        }
        return $div;
    },
    isAsync: function (func) { // 判断是否为异步函数
        return func.constructor.name === 'AsyncFunction';
    },
    eval: function (code, obj) { // 执行代码
        var result = new Function("obj", `return ${code}`)(obj);
        return result;
    },
    jsonToString: function (json, clearobject) {
        if (clearobject) {
            var clearJson = utility.cleanObject(json);
            return JSON.stringify(clearJson, null, "\t");
        }

        return JSON.stringify(json, null, "\t");
    },
    move(array, oldIndex, newIndex) {
        if (newIndex < 0 || newIndex > array.length) return;
        if (newIndex >= array.length) {
            let k = newIndex - array.length + 1;
            while (k--) {
                array.push(undefined);
            }
        }
        array.splice(newIndex, 0, array.splice(oldIndex, 1)[0]);
        return array;
    },
    cleanObject: function (obj) {
        var resultV = _.transform(obj, function (result, value, key) {
            if (!_.isUndefined(value)
                && !_.isNull(value)
                && !_.isEmpty(value)
                || _.isNumber(value) || _.isBoolean(value)) {
                if (_.isObject(value)) {
                    result[key] = utility.cleanObject(value);
                } else {
                    result[key] = value;
                }
            } else {
                //console.log("Uncleaned value: ", value, " with key: ", key);
            }
        });
        return resultV;
    },
    jsonToUrl: function (url, jsonData) {
        if (!jsonData) {
            return url;
        }
        var purl = $.url(url);
        const params = new URLSearchParams(purl.data.attr.query);
        const json = {};
        for (const [key, value] of params.entries()) {
            json[key] = value;
        }
        $.extend(json, jsonData);
        return `${url.split('?')[0]}?${$.param(json)}`;
    },
    // js获取图片的主要颜色
    getMainColor: function (imageUrl, opacity) {
        return new Promise((resolve, reject) => {
            const img = new Image();
            img.crossOrigin = "Anonymous";
            img.src = imageUrl;
            img.onload = () => {
                const canvas = document.createElement("canvas");
                canvas.width = img.width;
                canvas.height = img.height;
                const ctx = canvas.getContext("2d");
                ctx.drawImage(img, 0, 0);
                const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height).data;
                const colorMap = new Map();
                let totalR = 0, totalG = 0, totalB = 0, count = 0;
                for (let i = 0; i < imageData.length; i += 4) {
                    const r = imageData[i];
                    const g = imageData[i + 1];
                    const b = imageData[i + 2];
                    const color = `rgb(${r},${g},${b})`;
                    if (color !== 'rgb(0,0,0)' && color !== 'rgb(255,255,255)') {
                        totalR += r;
                        totalG += g;
                        totalB += b;
                        count++;
                    }
                }
                const avgR = Math.round(totalR / count);
                const avgG = Math.round(totalG / count);
                const avgB = Math.round(totalB / count);
                const mainColor = `rgba(${avgR},${avgG},${avgB},${opacity || 1})`;

                resolve(mainColor);
            };
            img.onerror = (err) => {
                reject(err);
            };
        });
    },
    isNullOrEmtpty: function (value) { // 判断是否为空或null
        return value === null || value === undefined || value === '';
    },
    triggerWindowResize: function () {
        if (typeof (Event) === 'function') {
            // 现代浏览器
            window.dispatchEvent(new Event('resize'));
        } else {
            // 兼容旧版IE
            var resizeEvent = window.document.createEvent('UIEvents');
            resizeEvent.initUIEvent('resize', true, false, window, 0);
            window.dispatchEvent(resizeEvent);
        }
    },

    getArgs: function (func) { // 获取函数参数
        return (func + '')
            .replace(/[/][/].*$/mg, '') // strip single-line comments
            .replace(/\s+/g, '') // strip white space
            .replace(/[/][*][^/*]*[*][/]/g, '') // strip multi-line comments  
            .split('){', 1)[0].replace(/^[^(]*[(]/, '') // extract the parameters  
            .replace(/=[^,]+/g, '') // strip any ES6 defaults  
            .split(',').filter(Boolean); // split & filter [""]
    },
    getResult: function (func, params) { // 获取函数结果
        return _.zipObject(utility.getArgs(func), params);
    },
    asynTEST: async function (params, test) { // 异步测试
        return params;
        //return new Promise(resolve => {
        //    setTimeout(() => {
        //        resolve(params);
        //    }, 2000);
        //});
    },
    postJsonData: async function (url, params, ajaxOptions) {
        var headers = {
            'Content-Type': 'application/json'
        };
        var payload = JSON.stringify(params || {});
        ajaxOptions = $.extend({ type: 'POST', headers: headers, data: payload }, ajaxOptions || {});
        var res = await $.ajax(url, ajaxOptions);
        return res;
    },
    postData: async function (url, params, ajaxOptions) {

        ajaxOptions = $.extend({ type: 'POST', data: params }, ajaxOptions || {});
        var res = await $.ajax(url, ajaxOptions);
        return res;
    },
    exeAsyncFunction: async function (func, ...params) { // 执行异步函数
        var pName = utility.getArgs(eval(func));
        if (pName.length) {
            pName.push(`return await ${func}(${pName.join(',')});`);
            var result = {};
            if (params.length == 0) {
                result = await new AsyncFunction(...pName)();
            }
            if (params.length == 1) {
                result = await new AsyncFunction(...pName)(params);
            }
            else {
                result = await new AsyncFunction(...pName)(...params);
            }
            return result;
        }
        else {
            var result = await new AsyncFunction(`return await ${func}();`)();
            return result;
        }
    },
    // 根据方法名和参数执行方法
    executeMethod: function (methodName, params) {
        const method = utility.eval(methodName);
        if (method.constructor.name === 'AsyncFunction') {
            method(...params).then((result) => {
                //console.log(`Result of ${methodName}(${params.join(', ')}):`, result);
                return result;
            });
        } else {
            const result = method(...params);
            //console.log(`Result of ${methodName}(${params.join(', ')}):`, result);
            return result;
        }
    },

    //   // 定义方法数组
    //   const methods = [
    //     { name: 'utility.newId', params: [],childMethod:[ { name: 'utility.newName', params: [] ,condition:"result==null" },
    //     { name: 'utility.htmlEncode', params: ['<p>Test</p>'],condition:"result!=null" }] },
    //     { name: 'utility.newName', params: [] },
    //     { name: 'utility.htmlEncode', params: ['<p>Test</p>'] },
    //     { name: 'utility.htmlDecode', params: ['&lt;p&gt;Test&lt;/p&gt;'] },
    //   ],

    ExeMethod: (methods) => { // 执行函数
        methods.forEach((method) => {
            var result = executeMethod(method.name, method.params);
            if (method.childMethod) {
                method.childMethod.forEach((childMethod) => {
                    const func = new Function('result', `return ${childMethod.condition ?? 'true'}`);
                    const resultCondition = func(result);
                    if (resultCondition) {
                        const childResult = executeMethod(childMethod.name, childMethod.params);

                        //console.log(`childMethod Result of ${childMethod.name}(${childMethod.params.join(', ')}):`, childResult);
                    }
                });
            }
        })
    }
};
nameSpace.register(`${CONST.APP_NAME}.utility`, utility);
var eventManager = {
    on: function (element, event, fn) {

        var $element = $(element);
        if (!$element.attr('uid')) {
            $element.attr('uid', utility.newId());
        }
        var dataTag = `${event}_${$element.attr('uid')}`;
        if ($(parent).data(dataTag)) {
            $(parent).unbind(event, $(parent).data(dataTag));
        }
        var _fn = function (e, data) {
            eventManager._fire($element, fn, e, data)
        };
        $(parent).data(dataTag, _fn);
        $(parent).on(event, _fn);
    },
    SetBeforeEvent: function (fn) {
        $('body').data('BeforeEvent', fn);
    },
    _getBeforeEvent: function () {
        return $('body').data('BeforeEvent');
    },
    _fire: function ($element, fn, e, data) {
        if ($element && $element.is(':visible')) {
            var beforeEvent = this._getBeforeEvent();
            if (beforeEvent !== null && $.isFunction(beforeEvent)) {
                if (beforeEvent(e, data) === false) {
                    return;
                }
            }
            $.proxy(fn, $element)(e, data);
        }
    },
    publish: function (event, data) {
        $(parent).trigger(event, data);
    }
};
nameSpace.register(`${CONST.APP_NAME}.eventManager`, eventManager);
var initHelper = {
    vueCreateApp: function (config) {
        var app = Vue.createApp(config);
        Enumerable.from(Object.keys(DataV)).where(x => DataV[x].install).select(x => app.component(x, DataV[x])).toArray();
        app.use(TUI);
        app.use(ElementPlus);
        app.use(VXETable)
        //// 创建 Pinia 实例
        //const pinia = Pinia.createPinia();

        //// 将 Pinia 实例添加到全局 Vue 实例中
        //app.use(pinia);

        //app.directive("contenteditable", {
        //    bind(el, { arg, value, expression, modifiers }, vnode) {
        //        const innerValue = modifiers.dangerousHTML ? "innerHTML" : "innerText";
        //        if (arg) {
        //            el.contentEditable = value;
        //        } else {
        //            el.contentEditable = true;
        //        }
        //        const key = arg || expression;
        //        el.oninput = function (event) {
        //            vnode.context[key] = event.target[innerValue];
        //            el.dataset.comparison = event.target[innerValue];
        //        };
        //        el.onblur = function (event) {
        //            el[innerValue] = el.dataset[key];
        //        };
        //        el.dataset[key] = vnode.context[key];
        //        el[innerValue] = vnode.context[key];
        //        return;
        //    },
        //    componentUpdated: function (
        //        el,
        //        { arg, modifiers, value, expression },
        //        vnode
        //    ) {
        //        const innerValue = modifiers.dangerousHTML ? "innerHTML" : "innerText";
        //        if (arg) {
        //            el.contentEditable = value;
        //        } else {
        //            el.contentEditable = true;
        //        }
        //        const key = arg || expression;
        //        const val = vnode.context[key];
        //        el.dataset[key] = val;
        //        if (val !== el.dataset.comparison) {
        //            el[innerValue] = val;
        //        }
        //        return;
        //    }
        //});
        //https://github.com/hl037/vue-contenteditable
        app.component("contenteditable", contenteditable);
        for (const [key, component] of Object.entries(ElementPlusIconsVue)) {
            app.component(key, component)
        }
        //app.use(VeeValidate, {
        //    events: 'change'     //这里的events是触发事件，例如失去焦点验证，我这里用的是输入改变校验});
        //});
        return app;
    },

    vueLoadCom: function (app, comName, url) {
        app.component(comName, Vue.defineAsyncComponent((x, y, z) => {
            //return new Promise((resolve, reject) => {
            //    resolve({
            //        template: '<div>I am async!</div>',
            //    })
            //});
            return $.get(url)
                .then(response => {
                    var comelement = $(`<div>${response}</div>`)
                    var initScript = comelement.find("script[tag='comconfig']");
                    if (initScript.length == 1) {
                        var setupConfig = new Function(`${(initScript).text()}; return comConfig;`)();
                        setupConfig.template = comelement.find("template").html();
                        //return {
                        //    template: '<div>I am async!</div>',
                        //}
                        return setupConfig;
                    }
                });
        }));
    },
    vueInit: function (element, model) {
        var el = $(element).get(0);

        var config = {
            data() {
                return { parentModel: dom.getModel(element) }
            },
            setup(prop) {
                var obj = {};
                var attrName = `${actionHelper.tagAttrName("vueInit", "init")}`.toLowerCase();
                if ($(element).attr(attrName)) {
                    obj = JSON.parse($(element).attr(attrName));
                }
                else {
                    obj = dom.getModel(element);
                }

                var model = el.model || model || Vue.ref(obj);

                return { model };
            }
        };
        if (el.setup) {
            config.setup = el.setup;
        }

        var initScript = $(element).find("script[tag='appconfig']");
        if (initScript.length == 1) {
            var setupConfig = new Function('element', `${(initScript).text()}; return appConfig;`)(element);
            if (setupConfig) {
                config = $.extend(config, setupConfig);
            }
        }
        var app = initHelper.vueCreateApp(config);
        $.ajax({
            url: '/VueComponent/FormItem/GetComs',
            async: false,
            success: function (coms) {
                coms.forEach(c => { initHelper.vueLoadCom(app, c.name, c.url); });
            }
        });
        //initHelper.vueLoadCom(app, "TTest", "VueComponent/FormItem/CheckBox");
        //initHelper.vueLoadCom(app, "t-label-wrapper", "VueComponent/FormItem/LabelWrapper");
        $(element).find("script").remove();
        app.mount($(element).get(0))
        $(element).data("app", app);
        return app;
    },
    vueUnmount: function (element) {

        var el = $(element).get(0);
        el.__vue_app__.unmount(el);
    },
    winMax: function (element) {
        if ($(element).closest(".layui-layer-page").attr("isfirst") == "true")
            return;
        var $find = $(element).closest(".layui-layer-page").find(".layui-layer-max");
        if (!$find.hasClass("layui-layer-maxmin")) {
            $(element).closest(".layui-layer-page").attr("isfirst", "true")
            $find.trigger("click");
        }

    },
    winStyle: function (element, css) {
        if ($(element).closest(".layui-layer-page").attr("isfirst") == "true") {
            //setTimeout(() => utility.triggerWindowResize(), 50);
            return;
        }

        var $find = $(element).closest(".layui-layer-page");
        var attrName = `${actionHelper.tagAttrName("winStyle", "init")}`.toLowerCase();
        if ($(element).attr(attrName)) {
            $(element).closest(".layui-layer-page").attr("isfirst", "true")
            obj = JSON.parse($(element).attr(attrName));
            $find.css(obj);

            if (obj.autoheight) {
                $(element).closest(".layui-layer-content").height($find.height() - 40);
            }

        }
        setTimeout(() => utility.triggerWindowResize(), 50);

    },
    updateEl: function (element) {
        var attrName = `${actionHelper.tagAttrName("updateEl", "init")}`.toLowerCase();
        //var url = "";
        //if ($(element).attr(attrName)) {
        //    obj = JSON.parse($(element).attr(attrName));
        //    $find.css(obj);
        //}
        dom.updateEl(element, $(element).attr(attrName));
    }

};
nameSpace.register(`${CONST.APP_NAME}.initHelper`, initHelper);
var dom = {};
dom.unmount = function (element) {

    $(element).find("[data-v-app]").each(function () {
        var el = $(this).get(0);
        try {
            el.__vue_app__?.unmount(el);
        }
        catch {

        }

    });
    var el = $(element).get(0);
    el.__vue_app__?.unmount(el);
}
dom.GET_VUE_APP = function (element) {
    return $(element).get(0).__vue_app__;
}
dom.openWin = function (title, imgsrc, options) {
    imgsrc = imgsrc || '/img/icon1/100004092.png'
    var $content = $(`<div id="${utility.newId()}" style="height:100%">
    <div class="flex flex-col justify-center gap-2 place-items-center w-full h-full justify-items-center gap-4">
     <img src="${imgsrc}" class="h-32 w-32 ">
     <div class="relative h-32 ">
        <div class="loadwrapper">
        <div class="circle"></div>
        <div class="circle"></div>
        <div class="circle"></div>
        <div class="shadow"></div>
        <div class="shadow"></div>
        <div class="shadow"></div>
        <span>Loading</span>
    </div>
     </div>
  

    </div>
    </div>`).appendTo("body");
    var args = arguments;
    var win = null;
    //layer.zIndex = 2000;
    layui.layer.config({ zIndex: 1000 })
    var opt = $.extend({
        type: 1, // page 层类型
        area: ['800px', '600px'],
        shade: 0.6, // 遮罩透明度
        shadeClose: true, // 点击遮罩区域，关闭弹层
        maxmin: true, // 允许全屏最小化
        // zIndex: layer.zIndex,
        //minStack:false,
        success: function (layero, index, that) {
            layer.setTop(layero); // 重点 2 --- 保持选中窗口置顶
            //layero.closest(".layui-layer-page").addClass("bg-gray-300");
            layero.closest(".layui-layer-page").css("max-height", $(window).height());
            utility.getMainColor(imgsrc, 1).then(x => { layero.closest(".layui-layer-page").css("backgroundColor", x); })
            //// 记录索引，以便按 esc 键关闭。事件见代码最末尾处。
            //layer.escIndex = layer.escIndex || [];
            //layer.escIndex.unshift(index);
            // 选中当前层时，将当前层索引放置在首位
            //setTimeout(() => { $content.html(""); $content.addClass("bg-white") }, 5000);
            layero.off('mousedown').on('mousedown', function (e) {
                //if ($.contains(layero, e.target)
                if ($(e.target).hasClass("layui-icon-down")) return false;
                var zIndex = parseInt(layero[0].style.zIndex)
                if (zIndex < layer.zIndex) {
                    layer.zIndex++;
                    layer.index++;
                    layero.css("zIndex", layer.zIndex);
                }



            });
            $(layero).data("win", that);
            win = that;
            eventManager.publish("openwin", { args: utility.getResult(dom.openWin, args), com: that });
        },
        resizing: function () {
            //_.debounce(function () {
            //    utility.triggerWindowResize();
            //}, 200)();

        },
        moveEnd: function (layero) {

        },
        restore: function (layero, index, that) {
            layero.attr("isfull", "false");
            //utility.triggerWindowResize();
        },
        full: function (layero, index, that) {
            var isfix = layero.css('position') === 'fixed';
            //utility.triggerWindowResize();
            layer.style(index, {
                top: isfix ? 0 : 0, //win.scrollTop(),
                left: isfix ? 0 : 0,// win.scrollLeft(),
                height: 'calc(100% - 40px)',
                width: '100%'
            }, true);
            layero.attr("isfull", "true");

        },
        end: function (layero, index, that) {
            dom.unmount($content);
            $content.remove();
            eventManager.publish("closewin", { args: utility.getResult(dom.openWin, args), com: win });
        },
        min: function (layero, index, that) {
            layero.attr("isfull")
            setTimeout(() => { layero.addClass("hidden"); }, 0);


            // do something
            // return false; // 阻止默认最小化
        },
        anim: 1, // 0-6 的动画形式，-1 不开启
        content: $content
    }, options);
    var index = layer.open(opt);

    layer.title($(`<div>
        <div class="w-full flex flex-row text-white text-stronger content-start gap-1 place-items-center">
            <img src="${imgsrc}" class="h-6 w-6 ">
            <div class="text">${title}</div>
        </div>
        </div>`).html(), index);

    return $content;
};
dom.openWinUrl = function (title, imgsrc, options, url) {
    var $content = dom.openWin(title, imgsrc, options);
    setTimeout(() => {
        dom.updateEl($content, url);
    }, 10);
    return $content;

}
dom.setActiveWin = function (index) {
    var layero = $(`#layui-layer${index}`);
    if (layero.hasClass("hidden")) {
        layer.restore(index);
        if (layero.attr("isfull") == "true") {
            layero.find(".layui-layer-max").trigger("click");
        }


        layero.removeClass("hidden");
        layero.trigger("mousedown");

    }
    else {

        layero.find('.layui-layer-min').trigger("click");
    }

}

dom.updateEl = async function (element, url, urlparams, ajaxOptions, targetInfo) {
    var $parent = $(targetInfo?.currentTarget).closest("[data-url]");
    var $el = $parent.find(element);
    if (!$el.length) {
        $el = $(element);
    }
    if (!$el.length) {
        $el = $parent;
    }
    if (!$el.length) {
        alert('Can not find element');
        return;
    }
    var result = utility.getResult(dom.updateEl, arguments);
    var headers = {
        'Content-Type': 'application/json'
    };
    var payload = JSON.stringify({});
    ajaxOptions = $.extend({ type: 'POST', headers: headers, data: payload }, ajaxOptions || {});
    var res = await $.ajax(utility.jsonToUrl(url, urlparams), ajaxOptions);
    $el.addClass("bg-white");

    dom.unmount($el);
    $el.attr("data-url", url);
    $el.html(res);

    dom.init($el);
    //console.log(res);

};
dom.getVueInstance = function (element, targetInfo) {
    var $$app = $(element || targetInfo?.currentTarget).closest("[data-v-app]");
    if ($$app.length == 0) {
        $$app = $(element).find("[data-v-app]");
    }
    var result = null;
    if ($$app.length > 0) {
        result = dom.GET_VUE_APP($$app)._instance.proxy;
    }
    return result;
};
dom.getModel = function (element, targetInfo) {

    var model = dom.getVueInstance(element, targetInfo)?.model || {};
    return model;
};
dom.postData = async function (element, url, urlparams, event, ajaxOptions, targetInfo) {
    var data = dom.getModel(element, targetInfo);
    ajaxOptions = ajaxOptions || {}
    // 将JSON对象转换为字符串
    var payload = JSON.stringify(data);
    // 设置请求头
    var headers = {
        'Content-Type': 'application/json'
    };
    urlparams = $.extend({ _Event: event || "Load" }, urlparams || {});
    var $el = $(element || targetInfo?.currentTarget).closest("[data-url]");
    url = url || $el.attr("data-url");
    try {
        var res = await $.ajax(utility.jsonToUrl(url, urlparams), $.extend({
            type: 'POST',
            data: payload,
            headers: headers,
        }, ajaxOptions));
        // console.log('Response:', res);
        var responseType = typeof res; // 获取响应类型

        if (responseType === 'object') {
            var instance = dom.getVueInstance(element, targetInfo);
            _.set(instance, "model", res);
        } else if (responseType === 'string') {
            var $element = $el;
            if (ajaxOptions.usePublicDiv) {
                $element = $(utility.publicDIV());
            }
            $element.addClass("bg-white");
            dom.unmount($element);
            $element.attr("data-url", url);

            $element.html(res);

            dom.init($element);
        }
    }
    catch (err) {
        console.log(err);
    }


    //console.log(res);

};
dom.reload = async function (element, event, targetInfo) {
    dom.postData(element, null, null, event, null, targetInfo);
}
dom.confirmPostData = async function (element, confimOptions, url, urlparams, event, ajaxOptions, targetInfo) {
    ajaxOptions = ajaxOptions || {}
    ajaxOptions.usePublicDiv = true;
    layer.confirm('你确定吗?', { icon: 3, title: '提示', btn: ['确定', '取消'] }, function (index) {

        var p = dom.postData(element, url, urlparams, event, ajaxOptions, targetInfo);
        p.then(() => { dom.reload(null, null, targetInfo) });
        layer.close(index);
    });
}

dom.init = function (element) {
    initHelper.asEnumerable().toArray().forEach(f => {
        $(element).find(`[${actionHelper.tagAttrName(f.key, "init")}]`.toLowerCase()).toArray().forEach(e => {
            f.value(e);
        });
    });
}
nameSpace.register(`${CONST.APP_NAME}.dom`, dom);
var actionHelper = {

    getElementByEvent: function ($event) {
        return $event.currentTarget;
    },
    getComInstance: function ($event) {
        return actionHelper.getElementByEvent($event).instance;
    },
    tagAttrName: function (fnName, eventName) {
        return `t-${eventName}-${fnName}`.toLowerCase();
    },
    getOptions: function (fnName, eventName, $event) {
        // var options = JSON.parse($($event.currentTarget).attr(actionHelper.tagAttrName(fnName, eventName, $event)) || "{}");
        // var instance = actionHelper.getComInstance($event);
        // var pa = Enumerable.from(_.keys(instance)).select(function (key) { return { key: key, value: instance[key] } }).toArray();
        // var tempFn = eval(`(${Enumerable.from(pa).select((key) => key.key).toArray().join(',')})=>{ return ${JSON.stringify(options)};}`);
        // result = tempFn.apply(instance, Enumerable.from(pa).select((key) => key.value).toArray());
        // return result;
        var el = actionHelper.getElementByEvent($event);
        if (el.binding) {
            var binding = el.binding.value;
            var options = binding.asEnumerable().where(x => fnName === x.action && eventName === x.event).firstOrDefault()?.options;
            return options;
        }
        else {
            var options = JSON.parse($($event.currentTarget).attr(actionHelper.tagAttrName(fnName, eventName, $event)) || "{}");

            return options;
        }
    },
    mapObjToFun: function (fn, object, $event) {
        let pNames = utility.getArgs(fn);
        var args = new Array(pNames.length);
        for (var i = 0; i < args.length; i++) {
            args[i] = _.get(object, pNames[i]);
        }
        return args;
    }, getParams: function (fn, fnName, eventName, $event) {
        var options = actionHelper.getOptions(fnName, eventName, $event);
        // var instance = actionHelper.getComInstance($event);
        var args = actionHelper.mapObjToFun(fn, options, $event);
        let pNames = utility.getArgs(fn);
        // var element = actionHelper.getElementByEvent($event);
        var index = pNames.indexOf('targetInfo');
        if (index >= 0) {
            args[index] = $event;
        }
        // if (pNames[0] === 'element') {
        //     args[0] = element;
        // }
        var result = fn.apply(dom, args);
        return result;
    },

}
$(function () {

    Enumerable.from(['change', 'click', 'dblclick', 'error', 'focus', 'select', 'mouseover']).forEach(eventName => {
        dom.asEnumerable().toArray().forEach(f => {
            $('body').on(eventName, `[${actionHelper.tagAttrName(f.key, eventName)}]`.toLowerCase(), function ($event) {

                $event.preventDefault();
                var result = actionHelper.getParams(f.value, f.key, eventName, $event);
                if (result) {
                    $($event.currentTarget).data(`result_${actionHelper.tagAttrName(f.key, eventName)}`, result);
                }

            });
        });
    })
});




