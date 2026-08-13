
/**
 * Copyright (c) 2009 Sergiy Kovalchuk (serg472@gmail.com)
 * 
 * Dual licensed under the MIT (http://www.opensource.org/licenses/mit-license.php)
 * and GPL (http://www.opensource.org/licenses/gpl-license.php) licenses.
 *  
 * Following code is based on Element.mask() implementation from ExtJS framework (http://extjs.com/)
 *
 */
; (function ($) {

    /**
     * Displays loading mask over selected element(s). Accepts both single and multiple selectors.
     *
     * @param label Text message that will be displayed on top of the mask besides a spinner (optional). 
     * 				If not provided only mask will be displayed without a label or a spinner.  	
     * @param delay Delay in milliseconds before element is masked (optional). If unmask() is called 
     *              before the delay times out, no mask is displayed. This can be used to prevent unnecessary 
     *              mask display for quick processes.   	
     */
    $.fn.mask = function (label, delay) {
        $(this).each(function () {
            if (delay !== undefined && delay > 0) {
                var element = $(this);
                element.data("_mask_timeout", setTimeout(function () { $.maskElement(element, label) }, delay));
            } else {
                $.maskElement($(this), label);
            }
        });
    };

    /**
     * Removes mask from the element(s). Accepts both single and multiple selectors.
     */
    $.fn.unmask = function () {
        $(this).each(function () {
            $.unmaskElement($(this));
        });
    };

    /**
     * Checks if a single element is masked. Returns false if mask is delayed or not displayed. 
     */
    $.fn.isMasked = function () {
        return this.hasClass("masked");
    };

    $.maskElement = function (element, label) {

        //if this element has delayed mask scheduled then remove it and display the new one
        if (element.data("_mask_timeout") !== undefined) {
            clearTimeout(element.data("_mask_timeout"));
            element.removeData("_mask_timeout");
        }

        if (element.isMasked()) {
            $.unmaskElement(element);
        }

        if (element.css("position") == "static") {
            element.addClass("masked-relative");
        }

        element.addClass("masked");

        var maskDiv = $('<div class="loadmask"></div>');

        //auto height fix for IE
        //if (navigator.userAgent.toLowerCase().indexOf("msie") > -1) {
        //    maskDiv.height(element.height() + parseInt(element.css("padding-top")) + parseInt(element.css("padding-bottom")));
        //    maskDiv.width(element.width() + parseInt(element.css("padding-left")) + parseInt(element.css("padding-right")));
        //}
        //
        ////fix for z-index bug with selects in IE6
        //if (navigator.userAgent.toLowerCase().indexOf("msie 6") > -1) {
        //    element.find("select").addClass("masked-hidden");
        //}

        element.append(maskDiv);

        if (label !== undefined) {
            var maskMsgDiv = $('<div class="loadmask-msg" style="display:none;text-align:center"></div>');
            maskMsgDiv.append(`<div class="spinner-border text-primary" role="status">
                                                </div><span class="m-2">${label}</span>`);
            element.append(maskMsgDiv);
            if (element.is("body") || element.is("html")) {
                var top = ($(window).height() - $(maskMsgDiv).height()) / 2;
                var left = ($(window).width() - $(maskMsgDiv).width()) / 2;
                var scrollTop = $(document).scrollTop();
                var scrollLeft = $(document).scrollLeft();
                $(maskMsgDiv).css({ position: 'absolute', 'top': top + scrollTop > 250 ? 250 : top + scrollTop, left: left + scrollLeft }).show();
            }
            else {
                var ele_padding = {
                    top: parseInt(element.css('padding-top')),
                    left: parseInt(element.css('padding-left')),
                    bottom: parseInt(element.css('padding-bottom')),
                    right: parseInt(element.css('padding-right'))
                },
                    ele_height = element.height() + ele_padding.top + ele_padding.bottom,
                    ele_width = element.width() + ele_padding.left + ele_padding.right

                maskDiv.css({
                    height: ele_height||'100%',
                    width: ele_width || '100%',
                    position: 'absolute',
                    top: '0',
                    left: '0'
                })

                //calculate center position
                var top = Math.round(ele_height / 2 - 55 / 2);
                if (top > 250) { top = 250; }

                maskMsgDiv.css("top", top + "px");
                maskMsgDiv.css("left", Math.round(ele_width / 2 - 160 / 2) + "px");
            }
            maskMsgDiv.show();
        }

    };

    $.unmaskElement = function (element) {
        //if this element has delayed mask scheduled then remove it
        if (element.data("_mask_timeout") !== undefined) {
            clearTimeout(element.data("_mask_timeout"));
            element.removeData("_mask_timeout");
        }

        element.find(".loadmask-msg,.loadmask").remove();
        element.removeClass("masked");
        element.removeClass("masked-relative");
        element.find("select").removeClass("masked-hidden");
    };

})(jQuery);
/////////////////////////////////////////////////////////////////////////
var commonJson = {
    "message_Confirm": "Confirm",
    "message_Waiting": "Please Wait",
    "message_DelConfirmTitle": "Are you Sure?",
    "message_Success": "Success",
    "message_DelConfirmDes": "Do you want to delete this record?",
    "notification_Add": "Add successful.",
    "notification_Message": "Message",
    "notification_Save": "Save success.",
    "notification_Error": "Error occurred.",
    "message_Timeout": "Request is time out!",
    "message_InUse": "InUse",
    "messageWaiting": "Please Wait…"
}
String.format = function () {
    var s = arguments[0];
    for (var i = 0; i < arguments.length - 1; i++) {
        var reg = new RegExp('\\{' + i + '\\}', 'gm');
        s = s.replace(reg, arguments[i + 1]);
    }

    return s;
};

String.prototype.replaceAll = function (s1, s2) {
    return this.replace(new RegExp(s1, 'gm'), s2);
};

String.prototype.startWith = function (s) {
    var reg = new RegExp('^' + s);
    return reg.test(this);
};

String.prototype.endWith = function (s) {
    var reg = new RegExp(s + '$');
    return reg.test(this);
};
String.prototype.DDLPropName = function () {
    var s = this;
    if (s && s !== '' && s.startWith('\\[') === false) {
        s = s.substring(s.indexOf("-") + 1).trim();
    }
    return s + "";
};
/*
 * Extend date methods
 */
Date.prototype.toJSON = function () {
    var timezoneOffsetInHours = -(this.getTimezoneOffset() / 60); //UTC minus local time
    var correctedDate = new Date(this.getFullYear(), this.getMonth(),
        this.getDate(), this.getHours(), this.getMinutes(), this.getSeconds(),
        this.getMilliseconds());
    correctedDate.setHours(this.getHours() + timezoneOffsetInHours);
    var iso = correctedDate.toISOString().replace('Z', '');

    return iso;// + sign + leadingZero + Math.abs(timezoneOffsetInHours).toString() + ':00';
}
Date.prototype.Format = function (fmt) {
    var o = {
        'M+': this.getMonth() + 1,
        'd+': this.getDate(),
        'h+': this.getHours(),
        'm+': this.getMinutes(),
        's+': this.getSeconds(),
        'q+': Math.floor((this.getMonth() + 3) / 3),
        'S': this.getMilliseconds()
    };

    if (/(y+)/.test(fmt)) {
        fmt = fmt.replace(RegExp.$1, (this.getFullYear() + '').substr(4 - RegExp.$1.length));
    }

    for (var k in o) {
        if (new RegExp('(' + k + ')').test(fmt)) {
            fmt = fmt.replace(RegExp.$1, (RegExp.$1.length === 1) ? (o[k]) : (('00' + o[k]).substr(('' + o[k]).length)));
        }
    }

    return fmt;
};


$.extend({
    getUrlVars: function (url) {
        if (typeof url === 'undefined') url = window.location.href

        var vars = [], hash;
        var hashes = url.slice(url.indexOf('?') + 1).split('&');
        for (var i = 0; i < hashes.length; i++) {
            hash = hashes[i].split('=');
            vars.push(hash[0]);

            var values = (hash[1] || '').split('#');
            vars[hash[0]] = values[0];
        }
        return vars;
    },
    getUrlVar: function (name, url) {
        return $.getUrlVars(url)[name];
    },
    par2Json: function (string, overwrite) {
        var obj = {}, pairs = string.split('&'), d = decodeURIComponent, name, value;
        $.each(pairs, function (i, pair) {
            pair = pair.split('=');
            name = d(pair[0]);
            value = d(pair[1]);
            obj[name] = overwrite || !obj[name] ? value : [].concat(obj[name]).concat(value);
        });
        return obj;
    }
});

/*
 * Custom event manager
 */
var EventManager = {
    on: function (element, event, fn) {
        var $element = $(element);
        if (!$element.attr('uid')) {
            $element.attr('uid', guid());
        }
        var dataTag = String.format('{0}{1}', event, $element.attr('uid'));
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

$.fn.myOn = function (event, fn) {
    $(this).each(function () {
        EventManager.on($(this), event, fn);
    });
};

$.fn.myFire = function (event, data) {
    $(this).each(function () {
        EventManager.publish(event, data);
    });
};

/*
 * Custom ajax load methods
 */
function myLoad(element, config) {
    var type = 'POST';
    if (config.data._Event === 'Load') {
        type = 'GET';
    }
    if (config.data && typeof (config.data) !== 'string') {
        config.data = $.param(config.data, true);
    }
    var options = {
        type: type,
        cache: false,
        timeout: 1000 * 60 * 10,
        beforeSend: function () {
            if (!config.hideMask) {
                //kendo.ui.progress($(element), true);
                $(element).mask(config.maskText || commonJson.message_Waiting);
            }
        },
        complete: function (jqXHR, status, responseText) {
            if (!config.hideMask) {
                //kendo.ui.progress($(element), false);
                $(element).unmask();
            }
        },
        error: function (response, textStatus, errorThrown) {
            if (textStatus == "timeout") {
               utility.alert("timeout");
            }
            else {
                $(element).html(response.responseText);
            }

            //utility.publicDIV().html(response.responseText);
        },
        success: function (html) {
            if ($(element).get(0)._beforeUpdate) {
                $(element).get(0)._beforeUpdate();
            }
           if (config.removeScript) {
                var keepscript = $(html).find('script.keepscript');
                var els = $(html).find(':not(style,script,link)');
                els.appendTo($(element));
                keepscript.appendTo($(element));
            } else {
                if (config.usePublicDIV) {
                    if (html && html.indexOf && html.indexOf("class='dbEntityValidationException'") > 0) {
                        //Todo:utility.alert(html, { maxWidth: "80%", minWidth: "40%" });
                    } else {
                        utility.publicDIV().find('*').remove();
                        utility.publicDIV().html(html);
                        setTimeout(function () {
                            utility.publicDIV().html("");
                        }, 500);
                    }
                    return;
                }

                if (config.updateDiv) {
                    var $updateDiv = $(element).find(config.updateDiv);
                    $updateDiv.html($(html).find(config.updateDiv).html());
                }
                else {
                    if (html && html.indexOf && html.indexOf("class='dbEntityValidationException'") > 0) {
                       utility.alert(html, { maxWidth: "80%", minWidth: "40%" });
                    } else {
                        $(element).find('*').remove();
                        $(element).html(html);
                    }
                }

            }

            if (window._init) {
                $.proxy(_init, $(element))();
            }

            _init = function () { };
            try {
                $.each(ActionHelper, function (key, f) {
                    f('myaction-' + key, $(element));
                });
            }
            catch (e) {
                console.log(e);
            }
            if ($(element).get(0)._callback) {
                $(element).get(0)._callback();
            }
        }
    };

    if (typeof config === 'string') {
        jQuery.extend(options, { url: config });
    } else {
        jQuery.extend(options, config);
    }

    return $.ajax(options);
}
; (function ($) {
    $.fn.serializeJson = function (doNotValidateInPage, withDisabled) {
        var $this = $(this);
        var els = $(this).find(':input').filter(function (index) {
            var ddd = $(this).closest("[data-url]");
            if (ddd.length === 0)
                return false;
            return ddd[0] === $this[0]
        });
        if (doNotValidateInPage) {
            els = $(this).find(':input');
        }
        var serializeObj = {};
        if (withDisabled) {
            arr = $(els).serializeArrayWithDisabled();
        }
        else {
            arr = $(els).serializeArray();
        }
        els.each(function (i, o) {
            if ($(o).attr("multiple") && $(o).attr("name")) {
                serializeObj[o.name] = [];
            }
        });
        $.map(arr, function (n, i) {
            if (serializeObj[n.name]) {
                if ($.isArray(serializeObj[n.name])) {
                    serializeObj[n.name].push(n.value);
                } else {
                    serializeObj[n.name] = [serializeObj[n.name], n.value];
                }
            } else {
                serializeObj[n.name] = n.value;
            }
        });
        return serializeObj;
    };
    var r20 = /%20/g,
        rbracket = /\[\]$/,
        rCRLF = /\r?\n/g,
        rsubmitterTypes = /^(?:submit|button|image|reset|file)$/i,
        rsubmittable = /^(?:input|select|textarea|keygen)/i;
    var rcheckableType = (/^(?:checkbox|radio)$/i);
    var formValuesChange = false,
        winStayTimeout;

    $.fn.serializeArrayWithDisabled = function () {
        return this.map(function () {

            // Can add propHook for "elements" to filter or add form elements
            var elements = jQuery.prop(this, "elements");
            return elements ? jQuery.makeArray(elements) : this;
        })
            .filter(function () {
                var type = this.type;

                // Use .is( ":disabled" ) so that fieldset[disabled] works
                return this.name &&
                    rsubmittable.test(this.nodeName) && !rsubmitterTypes.test(type) &&
                    (this.checked || !rcheckableType.test(type));
            })
            .map(function (i, elem) {
                var val = jQuery(this).val();

                return val === null ?
                    null :
                    jQuery.isArray(val) ?
                        jQuery.map(val, function (val) {
                            return { name: elem.name, value: val.replace(rCRLF, "\r\n") };
                        }) :
                        { name: elem.name, value: val.replace(rCRLF, "\r\n") };
            }).get();
    };

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
    $.fn.serializeEls = function () {
        var serializeObj = {};
        $(this).each(function () {
            if ($(this).attr("name")) {
                serializeObj[$(this).attr("name")] = $(this).val();
            }
        })
        $.extend(serializeObj, $(this).serializeJson(true));
        return serializeObj;
    };
    $.fn.myLoad = function (url, event, config, params) {
        var defers = new Array();
        $(this).each(function () {
            if (!$(this).attr('id')) {
                $(this).attr('id', utility.newCtrlId());
            }
            var elementId = this.id;
            var baseParams = { _ElementID: this.id, _Event: event || 'Load' };
            var data = {};
            if (config && config.data) {
                jQuery.extend(data, config.data);
            }
            jQuery.extend(data, baseParams);
            var purl = $.url(url);
            jQuery.extend(data, purl.data.param.query);
            jQuery.extend(data, params || {});
            var options = { url: purl.attr('path'), data: data, cache: false, originalUrl: url };
            config = config || {};
            jQuery.extend(config, options);
            if (config.usePublicDIV) {
                $(utility.publicDIV()).attr('data-url', url);
                $(utility.publicDIV()).data('url', url);
                $(utility.publicDIV()).attr('data-event', config.data._Event);
                $(utility.publicDIV()).data('event', config.data._Event);
            }
            else {
                $(this).attr('data-url', url);
                $(this).data('url', url);
                $(this).attr('data-event', config.data._Event);
                $(this).data('event', config.data._Event);
            }
            var defer = myLoad(this, config);
            $(this).data("myLoad", defer);
            defers[defers.length] = defer;
        });
        return defers.length === 1 ? defers[0] : defers;
    };

    $.fn.myReload = function (event, config, params) {
        $(this).each(function () {
            var url = $(this).data('url');
            return $(this).mySubmit(url, event, config, params);
        });
    };

    $.fn.mySubmit = function (url, event, config, params) {
        var formParams = {}
        config = config || {};
        params = params || {};
        formParams = $(this).serializeJson(config.doNotValidateInPage);
        jQuery.extend(params, formParams);
        jQuery.extend(config, { type: 'POST' });
        if (!url) {
            url = $(this).data('url');
        }

        return $(this).myLoad(url, event, config, params);
    };
})(jQuery);

/*
 * Others
 */
function guid() {
    return s4() + s4() + '-' + s4() + '-' + s4() + '-' + s4() + '-' + s4() + s4() + s4();
}

function s4() {
    return Math.floor((1 + Math.random()) * 0x10000)
        .toString(16)
        .substring(1);
}

var utility = {
    newCtrlId: function () {
        return 'Ctrl' + s4() + s4() + '-' + s4() + '-' + s4() + '-' + s4() + '-' + s4() + s4() + s4();
    },
    appendDiv: function (elementId) {
        if (typeof elementId === 'string') {
            var element = $(String.format('#{0}', elementId));

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
    getPageInfo: function (element) {
        return {
            $page: $(element).closest("[data-url]"),
            findPage: ($(element).closest("[data-url]").length > 0)
        };
    },
    notification: function (type, message, title, option) {

        toastr.options = {
            "closeButton": true,
            "debug": false,
            "newestOnTop": true,
            "progressBar": true,
            "positionClass": "toast-bottom-right",
            "preventDuplicates": true,
            "onclick": null,
            "showDuration": 300,
            "hideDuration": 100,
            "timeOut": 5000,
            "extendedTimeOut": 1000,
            "showEasing": "swing",
            "hideEasing": "linear",
            "showMethod": "fadeIn",
            "hideMethod": "fadeOut"
        }
        toastr[type](message, title, option);
    },
    alert:function (msg, options) {
        var deferred = $.Deferred();
        options = $.extend({
            title: '',
            width:'max-content',
            icon: '<i class="fal fa-info-circle text-info h1 m-0" ></i>',
            center:true,
        }, options || {});
        var $Model = utility.openWindow(options);
      
        $Model.find(".modal-body ").html(`<div class="card-body">${msg}</div><div class="card-footer p-1" style="display: flex;
    background-color: #f1f1f1;
    justify-content: flex-end;
}">
    <button class="btn btn-info waves-effect waves-themed"  data-dismiss="modal" >
<span class="fal fa-check mr-1"></span>
OK</button>
</div>`);
        $Model.bind("close", function () {
            deferred.resolve('close')
        });
        return deferred;
    },
    confirm: function (title,msg, options) {
        var deferred = $.Deferred();
        options = $.extend({
            title: title|| 'Are you sure?',
            width: 'max-content',
            icon: '<i class="fal fa-question text-info h1 m-0" ></i>',
            center: true,
        }, options || {});
        var $Model = utility.openWindow(options);

        $Model.find(".modal-body ").html(`<div class="card-body">${msg}</div><div class="card-footer p-1" style="display: flex;gap:5px;
    background-color: #f1f1f1;
    justify-content: flex-end;
}">
 <button class="btn btn-outline-danger waves-effect waves-themed btn_cancel"  >
<span class="fal fa-exclamation-circle mr-1"></span>
Cancel</button>
    <button class="btn btn-info waves-effect waves-themed btn_ok"   >
<span class="fal fa-check mr-1"></span>
OK</button>
</div>`);
        $Model.find(".card-footer .btn_cancel").on("click", () => { $Model.data("modal").hide(); deferred.resolve(false); });
        $Model.find(".card-footer .btn_ok").on("click", () => { $Model.data("modal").hide(); deferred.resolve(true); });
        return deferred;
    },
    openWindow: function (options, URL, event, config, params) {
        var Deferred=$.Deferred();
        var $Modal = utility.appendDiv(utility.newCtrlId());
        var bodyId = utility.newCtrlId();
        options = $.extend({
            title: '',
            width: '1000px',
            heigth: '200px',
            center:false,
            icon: '<i class="fal fa-window text-info h1 m-0" ></i>',
        }, options || {});
        $Modal.html(`<div class="modal-dialog win ${options.center ? "modal-dialog-centered" : ""}" style="max-width:${options.width};height:${options.heigth};min-width:200px;"  >
                                                    <div class="modal-content ">
                                                        <div class="panel-hdr ">
                                                            <h2 style="gap:5px" >${options.icon}<span class="wintitle"> ${options.title}</span></h5>
<div class="panel-toolbar">
 <button class="btn btn-panel bg-transparent fs-xl w-auto h-auto rounded-0 waves-effect waves-themed" data-dismiss="modal" aria-label="Close"><i class="fal fa-times"></i></button>

                                        </div>
                                                       
                                                        </div>
                                                        <div class="modal-body m-0 p-0" style="max-height:calc(90vh);min-height:100px;overflow-y: auto" id=${bodyId}>
                                                            
                                                        </div>
                                                       
                                                    </div>
                                                </div>`);
        $Modal.addClass("modal fade");
        var modal = new bootstrap.Modal($Modal.get(0), {
            backdrop: 'static',
            keyboard: false
        });
        $Modal.data('modal', modal);
        $Modal.on('shown.bs.modal', function () {
            if (URL) {
                $body.myLoad(URL, event, config, params);
            }
        })
        modal.show();
        $body = $(`#${bodyId}`);
        params = params || {};
        params._OpenModal = true;
        
        var $this = $(this);
        $Modal.on('hide.bs.modal', function (e) {
            if (e.target === this) {
                
                $Modal.trigger("close");
                $Modal.remove();
            }
        })
        return $Modal;

    },
};
var nameSpace = {
    register: function (fullNS, obj) {
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
var ActionHelper = {
    checkDataUrl: function (dom, container) {
        if ($(dom).closest("[data-url]").length) {
            if ($(dom).closest("[data-url]").get(0) != $(container).get(0)) {
                return false;
            }
        }
        return true;
    },
    initform: function (key, $this) {
        var $find = $this.find("[" + key + "]");
        if ($find.length > 0) {
            $find.validate({ meta: "validate" });
        }
    },
    winoptions: function (key, $this) {
        var $win = $this.closest(".win");
        var $find = $this.find("[" + key + "]")
        if ($win.length > 0) {
            debugger;
            if ($find.attr(key)) {
                var options = JSON.parse($find.attr(key));
                if (options.width) {
                    $win.css("width", options.width);
                    $win.css("max-width", options.width);
                }
                if (options.height) {
                    $win.css("height", options.height);
                }
                if (options.title) {
                    $win.find(".wintitle").text(options.title);
                }
            }
        }
    },
    changepostback: function (key, $this) {
        $this.off("change." + key).on("change." + key, "[" + key + "]", function () {
            if (ActionHelper.checkDataUrl(this, $this) == false) {
                return;
            }
            var opts = {
                config: {},
                event: $(this).attr("event") || $(this).attr("id") || "Load",
                params: $(this).data("params") || {},
                timeout: $(this).attr("timeout")
            };
            if ($(this).attr(key)) {
                setting = JSON.parse($(this).attr(key));
                opts = $.extend(opts, setting);
            }
            opts.params["TargetName"] = $(this).attr("Name");
            if (opts.timeout) {
                setTimeout(function () { $this.myReload(opts.event, opts.config || {}, opts.params || {}); }, parseInt(opts.timeout));
            }
            else {
                $this.myReload(opts.event, opts.config || {}, opts.params || {});
            }

            return false;
        });
    },
    reload: function (key, $this) {
        $this.off("click." + key).on("click." + key, "[" + key + "]", function () {
            if (ActionHelper.checkDataUrl(this, $this) == false) {
                return;
            }
            var opts = {
                config: {},
                event: $(this).attr("event") || "Load",
                params: $(this).data("params")
            };
            if ($(this).attr(key)) {
                setting = JSON.parse($(this).attr(key));
                opts = $.extend(opts, setting);
            }
            var $page = $this.closest("[data-url]");
            if ($page.length > 0) {
                $page.myLoad($page.data("url"), opts.event, opts.config || {}, opts.params || {});
            }
            else {
                window.location.reload();
            }

        });
    },
    postback: function (key, $this) {
        $this.off("click." + key).on("click." + key, "[" + key + "]", function () {
            if (ActionHelper.checkDataUrl(this, $this) == false) {
                return;
            }
            var opts = {
                config: ($(this).attr("config") ? JSON.parse($(this).attr("config")) : null) || {},
                event: $(this).attr("event") || $(this).attr("id") || "Load",
                params: $(this).data("params"),
                valid: $(this).hasClass("valid") || $(this).attr("id") === "Save",
                confirm: ($(this).attr("confirm") ? JSON.parse($(this).attr("confirm")) : null)
            };
            if ($(this).attr(key)) {
                setting = JSON.parse($(this).attr(key));
                opts = $.extend(opts, setting);
            }
            if (opts.valid) {
                
                if ($(this).data("beforeValidator")) {
                    if ($(this).data("beforeValidator")() === false) {
                        return false;
                    }

                }
                var validator = $this.find("form").valid('validate');
              
                if (validator) {
                        if ($(this).data("before")) {
                            if ($(this).data("before")() === false) {
                                return false;
                            }

                        }
                        if (opts.confirm) {
                            var confirm = utility.confirm(opts.confirm.title, opts.confirm.msg);
                            confirm.done(function (result) {
                                if (result) {
                                    $this.myReload(opts.event, opts.config || {}, opts.params || {});
                                }
                            });
                        }
                        else {
                            $this.myReload(opts.event, opts.config || {}, opts.params || {});
                        }

                    }
                
            }
            else {
                if (opts.confirm) {
                    var confirm = utility.confirm(opts.confirm.title, opts.confirm.msg);
                    confirm.done(function (result) {
                        if (result) {
                            $this.myReload(opts.event, opts.config || {}, opts.params || {});
                        }
                    });
                }
                else {
                    $this.myReload(opts.event, opts.config || {}, opts.params || {});
                }

            }

            return false;
        });
    },
    openwindow: function (key, $this) {
        $this.off("click." + key).on("click." + key, "[" + key + "]", function () {
            if (ActionHelper.checkDataUrl(this, $this) == false) {
                return;
            }
            var opts = {
                options: {title:'window'},
                event: "Load",
                config: {},
                params:{},
                refreshGrid: false,
                refresh: false,
            };

            if ($(this).attr(key)) {
                var setting = JSON.parse($(this).attr(key));
                opts = $.extend(opts, setting);
            }

            var $win = utility.openWindow(opts.options, opts.url, opts.event, opts.config, opts.params);
            if (opts.refresh) {
                $win.bind("close", function () {
                    $this.myReload();
                });
            }
            return false;
        });
    },
    closewindow: function (key, $this) {
        $this.off("click." + key).on("click." + key, "[" + key + "]", function () {
            if (ActionHelper.checkDataUrl(this, $this) == false) {
                return;
            }
            if ($(this).closest("[data-role=window]").length) {
                $(this).closest("[data-role=window]").data("kendoWindow").close();
            }
            return false;
        });
    },
    openlocation: function (key, $this) {
        $this.off("click." + key).on("click." + key, "[" + key + "]", function () {
            if (ActionHelper.checkDataUrl(this, $this) == false) {
                return;
            }
            var opts = {
                winoptions: $(this).data("winoptions"),
                url: $(this).attr("url"),
                event: $(this).attr("event") || "Load",
                config: {},
                params: $(this).data("params"),
                refreshGrid: false || $(this).hasClass('refreshGrid'),
                refresh: false || $(this).hasClass('refresh'),
            };

            if ($(this).attr(key)) {
                var setting = JSON.parse($(this).attr(key));
                opts = $.extend(opts, setting);
            }

            window.location = opts.url;

            return false;
        });
    },
    updatecontainter: function (key, $this) {
        $this.off("click." + key).on("click." + key, "[" + key + "]", function () {
            if (ActionHelper.checkDataUrl(this, $this) == false) {
                return;
            }
            var opts = {
                containter: $(this).attr("containter"),
                url: $(this).attr("url"),
                config: {},
                event: $(this).attr("event") || "Load",
                params: $(this).data("params") 
            };
            if (!opts.params) {
                var p = $(this).find(">.data-params");
                if (p.length && p.text()) {
                    pp = JSON.parse(p.text());
                    opts.params = pp;
                }
            }
            if ($(this).attr(key)) {
                setting = JSON.parse($(this).attr(key));
                opts = $.extend(opts, setting);
            }
            var $containter = null;
            if (opts.containter) {
                if ($this.find(opts.containter).length === 1) {
                    $containter = $this.find(opts.containter);
                }
                else {
                    $containter = $(opts.containter);
                }
            }
            else {
                $containter = $this;
            }
            $containter.myLoad(opts.url, opts.event, opts.config, opts.params);
            //return false;
        });
    },
    trigger: function (key, $this) {
        $this.off("click." + key).on("click." + key, "[" + key + "]", function () {
            if (ActionHelper.checkDataUrl(this, $this) == false) {
                return;
            }
            if ($(this).closest("[data-url]").length) {
                if ($(this).closest("[data-url]").get(0) != $this.get(0)) {
                    return;
                }
            }
            var opts = {
                element: $(this).attr("element"),
                event: $(this).attr("event") || "click",
            };
            if ($(this).attr(key)) {
                setting = JSON.parse($(this).attr(key));
                opts = $.extend(opts, setting);
            }
            if ($this.find(opts.element).length) {
                $this.find(opts.element).trigger(opts.event);
            }
            else {
                $(opts.element).trigger(opts.event);
            }
        });
    },
    delete: function (key, $this) {
        $this.off("click." + key).on("click." + key, "[" + key + "]", function () {
            if (ActionHelper.checkDataUrl(this, $this) == false) {
                return;
            }
            var $target = $(this);
            var opts = {
                title: commonJson.message_DelConfirmTitle,
                msg: commonJson.message_DelConfirmDes,
                event: "Delete",
                url: $(this).attr("url"),
                params: $(this).data("params"),
                hideConfirm: $(this).attr("hideconfirm") || false,

            };
            if ($(this).attr(key)) {
                setting = JSON.parse($(this).attr(key));
                opts = $.extend(opts, setting);
            }
            var deletePost = () => {
                $this.myLoad(opts.url, opts.event, { usePublicDIV: true, type: 'POST' }, opts.params);
                
                $this.data("myLoad").then(function () { $this.myReload(); });
             };
            if (!opts.hideConfirm) {
                var confirm = utility.confirm(opts.title, opts.msg);
                confirm.done(function (result) {
                    if (result) {
                        deletePost();
                    }
                });
            }
            else {
                deletePost();
            }
            return false;
        });
    },
    postbackdelete: function (key, $this) {
        $this.off("click." + key).on("click." + key, "[" + key + "]", function () {
            if (ActionHelper.checkDataUrl(this, $this) == false) {
                return;
            }
            var opts = {
                title: commonJson.message_DelConfirmTitle,
                msg: commonJson.message_DelConfirmDes,
                event: "Delete"
            };
            if ($(this).attr(key)) {
                setting = JSON.parse($(this).attr(key));
                opts = $.extend(opts, setting);
            }
            var confirm = utility.confirm(opts.title, opts.msg);
            confirm.done(function (result) {
                if (result) {
                    $this.myReload(opts.event);
                }
            });
            return false;
        });
    },
    selectall: function (key, $this) {
        $this.off("change." + key).on("change." + key, "[" + key + "]", function () {
            if (ActionHelper.checkDataUrl(this, $this) == false) {
                return;
            }
            var opts = {
                selectClass: $(this).attr("selectClass")
            };
            if ($(this).attr(key)) {
                setting = JSON.parse($(this).attr(key));
                opts = $.extend(opts, setting);
            }
            var checked = $(this).prop("checked");
            $this.find(opts.selectClass).each(function (index, x) {
                if ($(x).is(":disabled") == false) {

                    $(x).prop("checked", checked);
                }
            });

            return false;
        });
    },
    setwinoptions: function (key, $this) {
        var $find = $this.find("[" + key + "]");
        if ($find.length > 0) {
            var win = $find.closest('.k-window-content').data('kendoWindow');
            if (win) {
                win._setOptions($find.data('winoptions'));
                if (!$find.data('winoptions').unExcuteCenter) {
                    win.center();
                }

            }

        }
    },   
    helper: function (key, $container) {
        //var $find = $container.find("[" + key + "]");
        //if ($find.length > 0) {
        //    $find.each(function () {
        //        var $this = $(this);

        //        var config = {};
        //        var $config = $container.find('[data-user-helper-parameter]');
        //        if ($config.length > 0) {
        //            config = $config.data('user-helper-parameter');
        //        }

        //        var attributes = {
        //            visible: typeof config.Visible === 'undefined' ? true : config.Visible,
        //            top: config.Top || 8,
        //            right: config.Right || 10,
        //            zIndex: config.ZIndex || 1
        //        };

        //        if (attributes.visible) {
        //            var url = '';
        //            if (!config.ParentPageUrl) {
        //                var $element = $this.closest('[data-url]');
        //                if ($element.length > 0) {
        //                    url = $element.attr('data-url');
        //                } else {
        //                    url = window.location.pathname;
        //                }
        //            } else {
        //                url = config.ParentPageUrl;
        //            }

        //            var style = {};
        //            style.position = 'absolute';
        //            style.top = attributes.top + 'px';
        //            style.right = attributes.right + 'px';
        //            style['z-index'] = attributes.zIndex;
        //            $this.css(style);
        //            $this.load('/Config/UserHelper/RenderUserHelper?url=' + escape(url));
        //        }
        //    });
        //}
    },
    radioshowhide: function (key, $this) {
        $this.off("change." + key).on("change." + key, "[" + key + "]", function () {
            if (ActionHelper.checkDataUrl(this, $this) == false) {
                return;
            }
            var obj = $this.serializeJson(true);
            var $div = $this.find(kendo.format("[showdiv='{0}']", $(this).attr("name")));
            if (obj[$(this).attr("name")] === "True") {
                $div.show();
            }
            else {
                $div.hide();
            }
        });
        $this.find("[" + key + "]").trigger("change");

    },
    checkchange: function (key, $this) {
        var $find = $this.find("[" + key + "]"),
            attr = $find.attr(key);
        if (attr) {
            var param = JSON.parse(attr);
            if (param.saveCallback)
                $find.checkChange(eval(param.saveCallback), param.innerMaskElements, param.title, param.msg);
            else {
                if ($find.find('.btn-Submit'))
                    $find.checkChange(function () { $find.find('.btn-Submit').trigger('click'); }, param.innerMaskElements, param.title, param.msg);
            }
        }
    },
    myonreload: function (key, $this) {
        var $find = $this.find("[" + key + "]"),
            attr = $find.attr(key);

        if (attr) {
            $find.myOn(attr, () => {
                if ($find.find('[data-role="grid"]').length) {
                    $.each($this.find('[data-role="grid"]'), function (x) {
                        var $grid = $(this).data().kendoGrid;
                        if ($grid) {
                            $grid.dataSource.read();
                        }
                    });
                }

                else {
                    $this.myReload();
                }

            });
        }
    },
    select2: function (key, $this) {
        var $find = $this.find("[" + key + "]"),
            attr = $find.attr(key);
        $find.select2({
            placeholder: "Please select...",
            //width: 'resolve',
            allowClear: true
        });
        $this.find("[" + key + "]").on("change", function () { $(this).trigger("keyup") });
    },
  
};


$.extend($.validator.prototype, {
    showLabel: function (element, message) {
    }
});
$.extend($.validator.defaults, {

    ignore: ".ignore",
    errorClass: 'is-invalid',
    validClass: 'success',
    errorElement: 'span',
    highlight: function (element, errorClass, validClass) {
        var $element;
        if (element.type === 'radio') {
            $element = this.findByName(element.name);
        } else {
            $element = $(element);
        }
        $element.addClass(errorClass).removeClass(validClass);
        $element.parents("div.control-group").addClass("is-invalid");
    },
    unhighlight: function (element, errorClass, validClass) {
        var $element;
        if (element.type === 'radio') {
            $element = this.findByName(element.name);
        } else {
            $element = $(element);
        }
        $element.removeClass(errorClass).addClass(validClass);
        $element.parents("div.control-group").removeClass("is-invalid");
    },
    showErrors: function (errorMap, errorList) {
        $.each(this.successList, function (index, value) {
            var $el = $(value).closest("div.myctrl,form-group");
            $el.find(".invalid-feedback").remove();
        });
        $.each(errorList, function (index, value) {
            var $el = $(value.element).closest("div.myctrl,form-group");
            $el.find(".invalid-feedback").remove();
            $(`<div class="invalid-feedback d-block"><i class="fal fa-info-circle mr-1"></i>${value.message}</div>`).appendTo($el);
        });
        //$.each(this.successList, function (index, value) {
        //    var $el = $(value).closest("div.myctrl");
        //    $el.tooltip('dispose');
        //});
        //$.each(errorList, function (index, value) {
        //    var $el = $(value.element).closest("div.myctrl");
        //    $el.tooltip('dispose');
        //   var tooltip = $el.tooltip({
        //       trigger: 'manual',
        //       container: $el,
        //       title: value.message,
        //       placement: 'top',
        //    });
        //    //                    tooltip.data('bs.Tooltip').options.content = value.message;
        //    $el.tooltip('show');
        //});
        this.defaultShowErrors();
    }
});







