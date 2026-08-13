

(function () {
    const appName = 'tAPP';
    const elEventPrefix = 'el';
    const targetEventPrefix = 'target';
    var appTriggerEvent = function (funcName, data, el, targetInfo, action) {
        var elEventName = `${appName}_${funcName}_${elEventPrefix}${action || ''}`;
        var targetEventName = `${appName}_${funcName}_${targetEventPrefix}${action || ''}`;
        if (el && $(el).length) {
            $(el).trigger(elEventName, data);
        }
        if (targetInfo && targetInfo.currentTarget) {
            $(targetInfo.currentTarget).trigger(targetEventName, data);
        }
    };
    const urls = {
        pages: {
            detail: '/Pages/Detail',
        }
    }
    var nameSpace = {
        register: function (fullNS, obj) {
            fullNS = `${appName}.${fullNS}`;
            var nsArray = fullNS.split('.');
            var nameSpaceObj = window;
            var lastNameSpace = null;
            var lastNs = null;
            for (var i = 0; i < nsArray.length; i++) {
                var ns = nsArray[i];
                if (!nameSpaceObj[ns]) {
                    nameSpaceObj[ns] = {};
                }

                if (i === nsArray.length - 1) {
                    lastNameSpace = nameSpaceObj;
                    lastNs = ns;
                }
                nameSpaceObj = nameSpaceObj[ns];
            }
            if (obj) {
                lastNameSpace[lastNs] = obj;
            }
            return lastNameSpace[lastNs];
        }
    };
    var utility = {

        s4: function () {
            return Math.floor((1 + Math.random()) * 0x10000)
                .toString(16)
                .substring(1);
        },
        guid: function () {
            return this.s4() + this.s4() + '-' + this.s4() + '-' + this.s4() + '-' + this.s4() + '-' + this.s4() + this.s4() + this.s4();
        },
        newId: function () {
            return `Ctrl${this.guid()}`;
        },
        newName: function () {
            return 'name' + this.s4() + this.s4() + '_' + this.s4() + '_' + this.s4() + '_' + this.s4() + '_' + this.s4() + this.s4() + this.s4();
        },
        appendDiv: function (elementId) {
            if (typeof elementId === 'string') {
                var element = $(`#${elementId}`);

                if (element.length) {
                    return element;
                } else {
                    element = ($('<div>'))
                        .attr('id', elementId)
                        .appendTo('body');
                    return $(element);
                }
            } else {
                return $(elementId);
            }
        },
        HtmlEncode: function (value) {
            return $('<div/>').text(value).html();
        },
        HtmlDecode: function (value) {
            return $('<div/>').html(value).text();
        },
        publicDIV: function () {
            var $div = $('#PublicDiv');
            if ($div.length === 0) {
                $div = $('<div id="PublicDiv" style="display:none"></div>').appendTo('body');
            }
            return $div;
        },
        isAsync: function (func) {
            return func.constructor.name === 'AsyncFunction';
        },
        getArgs: function (func) {
            return (func + '')
                .replace(/[/][/].*$/mg, '') // strip single-line comments
                .replace(/\s+/g, '') // strip white space
                .replace(/[/][*][^/*]*[*][/]/g, '') // strip multi-line comments  
                .split('){', 1)[0].replace(/^[^(]*[(]/, '') // extract the parameters  
                .replace(/=[^,]+/g, '') // strip any ES6 defaults  
                .split(',').filter(Boolean); // split & filter [""]
        },
        getResult: function (func, params) {
            return _.zipObject(utility.getArgs(func), params);
        },
        getPageContainer: function (currentTargetEl) {
            var findEl = $(currentTargetEl).closest("[page-container]");
            if (findEl.length > 0) {
                return findEl.get(0);
            }
            else {
                findEl = $(currentTargetEl).closest(".tapp-dialog").find("[page-container]");
                if (findEl.length > 0) {
                    return findEl.get(0);
                }
                else {
                    alert('Can not find [page-container]');
                    return null;
                }
            }
        }
    };
    nameSpace.register("utility", utility);

    var EventManager = {
        on: function (element, event, fn) {
            var $element = $(element);
            if (!$element.attr('uid')) {
                $element.attr('uid', utility.newId());
            }
            var dataTag = `${event}_${$element.attr('uid')}`;
            if ($(this).data(dataTag)) {
                $(this).unbind(event, $(this).data(dataTag));
            }
            var _fn = function (e, data) {
                EventManager._fire($element, fn, e, data)
            };
            $(this).data(dataTag, _fn);
            $(this).on(event, _fn);
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
            $(this).trigger(event, data);
        }
    };
    nameSpace.register("eventManager", EventManager);

    var globalProperties = () => __tApp.config.globalProperties;
    var AJAX = () => __tApp.config.globalProperties.$request;
    var vue = {
        globalProperties: globalProperties,
        AJAX: AJAX
    }
    nameSpace.register("vue", vue);
    var dom = {
        animeShow: function (element, show, options, targetInfo) {
            var result = utility.getResult(dom.animeShow, arguments);
            var $this = $(element);
            var el = $this.get(0);
            options = options || {};

            var dir = `margin-${options.dir || 'left'}`;
            var opts = $.extend({ duration: 500, easing: 'linear' }, options || {});
            $(el).css("z-index", -999);
            if (show) {
                opts[dir] = "0";
                anime($.extend({
                    targets: el, begin: function (anim) {
                        $(el).show();

                    }, complete: function (anim) {
                        $(el).removeCss("z-index");
                        appTriggerEvent('animeShow', result, el, targetInfo);
                    }
                }, opts));
            } else {
                opts[dir] = `-${$this.width()}px`;
                anime($.extend({
                    targets: el, begin: function (anim) {
                    }, complete: function (anim) {
                        $(el).hide();
                        $(el).removeCss("z-index");
                        appTriggerEvent('animeShow', result, el, targetInfo);
                    }
                }, opts));
            }
        },
        anime: function (element, options, targetInfo) {
            options.targets = element || targetInfo.currentTarget;
            anime(options);
        },
        loadCom: function (element, options, targetInfo) {
            var el = $(element).get(0);
            var result = utility.getResult(dom.loadCom, arguments);
            const __dynamic = globalProperties().$dynamicCom(el, options.component,
                options.modelName,
                options.options,
                options.childrenCtrls,
                options.parentModelInfo,
                options.paramaterData,
            );
            appTriggerEvent('loadCom', result, el, targetInfo);
            $(el).data('__dynamic', __dynamic);
        },
        openWindow: function (options, closeCallback, targetInfo) {
            var result = utility.getResult(dom.openWindow, arguments);
            var $Div = utility.appendDiv(utility.newId());
            options = options || {};
            options.component = 'TDialog';
            $Div.loadCom(options, targetInfo);
            let $Modal = $Div.find(".modal");
            var myModal = new bootstrap.Modal($Modal, {
                keyboard: false
            });
            myModal.show();
            $Modal.on('hidden.bs.modal', function (event) {
                $Div.remove();
                if (closeCallback) {
                    closeCallback(result);
                }
                appTriggerEvent('openWindow', result, null, targetInfo, 'close');
            })
            appTriggerEvent('openWindow', result, null, targetInfo);
            return $Modal.find(".modal-body");
        },
        updateEl: async function (element, configOptions, dataOptions, defaultData, targetInfo) {
            var el = $(element).get(0);
            var result = utility.getResult(dom.updateEl, arguments);
            var data = await AJAX().request(dataOptions);
            var options = await AJAX().request(configOptions);


            options.parentModelInfo = data;
            if (!dataOptions.data.id && defaultData) {
                options.parentModelInfo = { ...defaultData };
            }
       
            $(el).attr("data-url", dataOptions.url);
            dom.loadCom(el, options, data, targetInfo);
            appTriggerEvent('updateEl', result, null, targetInfo);

        },
        openWindowByUrl: async function (options, configId, dataUrl, dataId, defaultData, closeCallback, targetInfo,) {
            var result = utility.getResult(dom.openWindowByUrl, arguments);
            var configOptions = {
                method: 'post',
                url: urls.pages.detail,
                data: { id: configId }
            };
            if (configId.indexOf('.json')) {
                configOptions.method = 'get';
                configOptions.url = configId;
            }
            var dataOptions = {
                method: 'post',
                url: dataUrl + 'get',
                data: { id: dataId }
            };
            var element = dom.openWindow(options, closeCallback, targetInfo);
            await dom.updateEl(element, configOptions, dataOptions, defaultData, targetInfo);
            appTriggerEvent('openWindowByUrl', result, null, targetInfo);
        },
        pageReload: async function (action, targetInfo) {
            var result = utility.getResult(dom.pageReload, arguments);
            var el = targetInfo.currentTarget;
            // if (element && element.length) {
            //     el = $(element).get(0);
            // }
            el = utility.getPageContainer(el);
            if (el.pageContainer) {
                if (el.pageContainer && el.pageContainer.reload) {
                    el.pageContainer.reload(action);
                }
                else {
                    alert('Can not find reload');
                    return;
                }
            }
            else {
                alert('Can not find pageContainer');
                return;
            }
            appTriggerEvent('pageReload', result, null, targetInfo);
            return result;

        },
        pagePostBack: async function (action, data, callback, closeWindow, targetInfo) {
            var result = utility.getResult(dom.pagePostBack, arguments);
            var el = targetInfo.currentTarget;
            // if (element && element.length) {
            //     el = $(element).get(0);
            // }
            // if (data){
            //     if (el.instance && el.pageContainer.postBack) {
            //         el.instance.postBack(action,data);
            //     }
            //     else{
            //         alert('Can not find pageContainer or postback');
            //     }
            // }
            el = utility.getPageContainer(el);
            if (data && el.pageContainer && el.postBack) {
                await el.pageContainer.postBack(action, data);
                if (callback) {
                    el.pageContainer.callback();
                }
            }
            else if (el.pageContainer) {
                if (el.pageContainer && el.pageContainer.postBack) {
                    await el.pageContainer.postBack(action, data);
                    if (callback) {
                        callback();
                    }
                }
                else {
                    alert('Can not find reload');
                    return;
                }
            }
            else {
                alert('Can not find pageContainer');
                return;
            }
            if (closeWindow) {
                bootstrap.Modal.getInstance($(el).closest(".tapp-dialog")).hide()
            }
            appTriggerEvent('pagePostBack', result, null, targetInfo);
            return result;

        },
        updateProp: function (element, propPath, data, targetInfo) {
            var el = element || targetInfo.currentTarget
            var result = utility.getResult(dom.updateProp, arguments);
            // debugger;
            if (el.instance) {
                _.set(el.instance, propPath, data);
                el.instance.$forceUpdate();
                appTriggerEvent('updateProp', result, null, targetInfo);
            }
        },
        myFire: function (element, event, data, targetInfo) {
            var el = element || targetInfo.currentTarget
            var result = utility.getResult(dom.myFire, arguments);

            EventManager.publish(event, data);

        },
        myOn: function (element, event, fn, targetInfo) {
            var el = element || targetInfo.currentTarget
            var result = utility.getResult(dom.myOn, arguments);
            if (el.instance) {
                EventManager.on($(instance), event, fn);
            }
            else {
                EventManager.on($("body"), event, fn);
            }
        },

    }
    nameSpace.register("dom", dom);
    dom.asEnumerable().toArray().forEach(x => {

        var params = utility.getArgs(x.value);
        if (params[0] === 'element') {
            $.fn[x.key] = function wrap() {
                var args = new Array(arguments.length + 1);

                for (var i = 0; i < arguments.length; i++) {
                    args[i + 1] = arguments[i];
                }
                $(this).each(function () {
                    args[0] = this;
                    return x.value.apply(dom, args);
                });
            };
        }
        else {
            $.fn[x.key] = x.value;
        }

    });
    // $.fn.myOn = function (event, fn) {
    //     $(this).each(function () {
    //         EventManager.on($(this), event, fn);
    //     });
    // };

    // $.fn.myFire = function (event, data) {
    //     $(this).each(function () {
    //         EventManager.publish(event, data);
    //     });
    // };
    $.fn.removeCss = function (options) {
        var type = typeof (options);
        if (type === "string") {
            this.each(function () {
                var style = $(this).attr("style");
                if (!style) return;
                var arr = style.split(";");
                style = "";
                for (var i = 0; i < arr.length; i++) {
                    if ($.trim(arr[i]) == "") {
                        continue;
                    }
                    var att = arr[i].split(":");
                    if ($.trim(att[0]) == $.trim(options)) {
                        continue;
                    }
                    style += $.trim(arr[i]) + ";";
                }
                $(this).attr("style", style);
            });
        } else if ($.isArray(options)) {
            this.each(function () {
                var style = $(this).attr("style");
                if (!style) return;
                var arr = style.split(";");
                style = "";
                for (var i = 0; i < arr.length; i++) {
                    for (var j = 0; j < options.length; j++) {
                        if ($.trim(arr[i]) == "") {
                            break;
                        }
                        var att = arr[i].split(":");
                        if ($.trim(att[0]) == $.trim(options[j])) {
                            arr[i] = "";
                            continue;
                        }
                    }
                }
                for (var i = 0; i < arr.length; i++) {
                    if ($.trim(arr[i]) != "") {
                        style += $.trim(arr[i]) + ";";
                    }
                }
                if ($.trim(style) == "") {
                    $(this).removeAttr("style");
                } else {
                    $(this).attr("style", style);
                }
            });
        }
    };
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
            // var tempFn = eval(`(${Enumerable.from(pa).select((key) => key.key).toArray().join(',')})=>{ debugger;return ${JSON.stringify(options)};}`);
            // result = tempFn.apply(instance, Enumerable.from(pa).select((key) => key.value).toArray());
            // return result;
            var el = actionHelper.getElementByEvent($event);
            var binding = el.binding.value;
            var options = binding.asEnumerable().where(x => fnName === x.action && eventName === x.event).firstOrDefault()?.options;
            return options;
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
            var instance = actionHelper.getComInstance($event);
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
        //https://www.w3school.com.cn/jquery/jquery_ref_events.asp
        Enumerable.from(['change', 'click', 'dblclick', 'error', 'focus', 'select', 'mouseover']).forEach(eventName => {
            dom.asEnumerable().toArray().forEach(f => {
                $('body').on(eventName, `[${actionHelper.tagAttrName(f.key, eventName)}]`.toLowerCase(), function ($event) {
                    $event.preventDefault();
                    actionHelper.getParams(f.value, f.key, eventName, $event);
                });
            });
        })
    })

})($);


