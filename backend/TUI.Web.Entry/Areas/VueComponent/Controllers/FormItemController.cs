using Furion;
using Furion.ClayObject;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using NPOI.XSSF.UserModel.Helpers;
using SqlSugar;
using System.Reflection;
using TUI.Core.Models;
using TUI.Web.Entry.Controllers;
using TUI.Web.Entry.ViewModels;

namespace TUI.Web.Entry.Areas.Tools.Controllers
{
    
    [Area("VueComponent")]
    public class FormItemController : BaseController
    {
        public IActionResult Checkbox()
        {
            return View();
        }
        public IActionResult CheckboxGroup()
        {
            return View();
        }
        
       
        public IActionResult ColorPicker()
        {
            return View();
        }
        public IActionResult DatePicker()
        {
            return View();
        }
        public IActionResult DatePickerRange()
        {
            return View();
        }
        public IActionResult DateTimePicker()
        {
            return View();
        }
        public IActionResult DateTimePickerRange()
        {
            return View();
        }
        public IActionResult Input()
        {
            return View();
        }
        public IActionResult TextBox()
        {
            return View("Input");
        }
        public IActionResult TextArea()
        {
            return View("Input");
        }
        public IActionResult Password()
        {
            return View("Input");
        }
       
        public IActionResult InputNumber()
        {
            return View();
        }
        public IActionResult Radio()
        {
            return View();
        }
        public IActionResult RadioButton()
        {
            return View();
        }
        public IActionResult Rate()
        {
            return View();
        }
        public IActionResult Select()
        {
            return View();
        }
        public IActionResult SelectMultiple()
        {
            return View();
        }
        public IActionResult Slider()
        {
            return View();
        }
        public IActionResult Switch()
        {
            return View();
        }
        public IActionResult TimePicker()
        {
            return View();
        }
        public IActionResult TimePickerRange()
        {
            return View();
        }
        public IActionResult TimeSelect()
        {
            return View();
        }
        public IActionResult Transfer()
        {
            return View();
        }
        public IActionResult Upload()
        {
            return View();
        }
        public IActionResult JsonEditor()
        {
            return View();
        }
        public IActionResult HtmlEditor()
        {
            return View();
        }
        public IActionResult Cascader()
        {
            return View();
        }
        public IActionResult Chips()
        {
            return View();
        }
        
        public IActionResult GetComs()
        {
            var controllerTypes = new List<Type> { typeof(FormItemController),
            typeof(ComController),
            typeof(ContainerController),
            typeof(WrapperController),
            typeof(HtmlComController),
            };
            var result =( from controllerType in controllerTypes
                         from m in controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                         where m.Name != "GetComs" && !m.GetCustomAttributes(typeof(NonActionAttribute), true).Any()
                         select new { cat = "Com Item", name = $"T{m.Name}", comname = getComName(m.Name),
                             url = $"/VueComponent/{controllerType.Name.Replace("Controller","")}/{m.Name}" })
                             .ToList();

 
           
            return Json(result);
        }
        
        private string getComName(string input)
        {
            string output = string.Concat(input.Select((x, i) => i > 0 && char.IsUpper(x) ? "-" + char.ToLower(x) : x.ToString())).ToLowerInvariant();
            return $"t-{output}";
        }
    }
}
