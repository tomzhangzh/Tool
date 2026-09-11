# 学校管理（SchoolMgr, projectId=4）全量配置脚本
# 步骤：generate 表定义 → 保存 Summary/Filter/Detail 页 → 模板（List/Crud/TreeList/Home）→ 网页路由 → 桌面快捷方式
$ErrorActionPreference = "Stop"
$base = "http://localhost:5000"
$projId = 4

function Invoke-Post([string]$url, $body) {
    $json = $body | ConvertTo-Json -Depth 30 -Compress
    $r = Invoke-RestMethod -Uri "$base$url" -Method Post -ContentType "application/json; charset=utf-8" -Body $json
    if (-not $r.success) { throw "API 失败 $url : $($r.message)" }
    return $r
}
function Invoke-Get([string]$url) {
    $r = Invoke-RestMethod -Uri "$base$url" -Method Get
    if (-not $r.success) { throw "API 失败 $url : $($r.message)" }
    return $r
}

# ============ 1. 表元数据 ============
$tables = @(
    @{ Name = 'School';      Label = '学校';  Route = '/schools';      Icon = '🏫'; TreeList = $false },
    @{ Name = 'Teacher';     Label = '教师';  Route = '/teachers';     Icon = '👩‍🏫'; TreeList = $false },
    @{ Name = 'Grade';       Label = '年级';  Route = '/grades';       Icon = '📚'; TreeList = $false },
    @{ Name = 'Subject';     Label = '学科';  Route = '/subjects';     Icon = '📖'; TreeList = $false },
    @{ Name = 'Class';       Label = '班级';  Route = '/class-tree';   Icon = '🏠'; TreeList = $true  },
    @{ Name = 'Student';     Label = '学生';  Route = '/student-crud'; Icon = '🧑‍🎓'; TreeList = $false },
    @{ Name = 'Exam';        Label = '考试';  Route = '/exams';        Icon = '📝'; TreeList = $false },
    @{ Name = 'ExamSubject'; Label = '考试科目'; Route = '/examsubjects'; Icon = '🗂️'; TreeList = $false },
    @{ Name = 'Score';       Label = '成绩';  Route = '/scores';       Icon = '🏅'; TreeList = $false }
)

$pageInfo = @{}   # table -> @{ Sum=id; Fil=id; Det=id }

foreach ($t in $tables) {
    $table = $t.Name
    Write-Output "===== 处理表 $table ====="
    $def = (Invoke-Get "/api/dynproject/$projId/generate?table=$table").data
    $defJson = $def | ConvertTo-Json -Depth 30 -Compress

    # 汇总屏
    $sum = @{ projectId = $projId; name = "pg_${table}_Sum"; title = "$($t.Label)列表"; pageType = 'Summary'; tableName = $table; columnDefs = $defJson; isEnabled = $true; sortOrder = 1 }
    $r1 = Invoke-Post "/api/dynproject/page/save" $sum
    $sumId = $r1.data.Id
    Write-Output "  Summary Id=$sumId"

    # 筛选屏（Filter 页保留全部字段定义，运行时仅展示主键/外键/名称类筛选字段）
    $fil = @{ projectId = $projId; name = "pg_${table}_Fil"; title = "$($t.Label)筛选"; pageType = 'Filter'; tableName = $table; columnDefs = $defJson; isEnabled = $true; sortOrder = 2 }
    $r2 = Invoke-Post "/api/dynproject/page/save" $fil
    $filId = $r2.data.Id
    Write-Output "  Filter Id=$filId"

    # 细节屏
    $det = @{ projectId = $projId; name = "pg_${table}_Det"; title = "$($t.Label)详情"; pageType = 'Detail'; tableName = $table; columnDefs = $defJson; isEnabled = $true; sortOrder = 3 }
    $r3 = Invoke-Post "/api/dynproject/page/save" $det
    $detId = $r3.data.Id
    Write-Output "  Detail Id=$detId"

    # 汇总屏关联细节屏
    $sum2 = @{ id = $sumId; projectId = $projId; name = "pg_${table}_Sum"; title = "$($t.Label)列表"; pageType = 'Summary'; tableName = $table; columnDefs = $defJson; detailPageId = $detId; isEnabled = $true; sortOrder = 1 }
    Invoke-Post "/api/dynproject/page/save" $sum2 | Out-Null
    Write-Output "  汇总屏关联详情屏完成"

    $pageInfo[$table] = @{ Sum = $sumId; Fil = $filId; Det = $detId }
}

# ============ 2. 模板 ============
# 普通 List 模板（RenderView=RouteList）：学校/教师/年级/学科/考试/考试科目/成绩
$listTables = @('School','Teacher','Grade','Subject','Exam','ExamSubject','Score')
foreach ($table in $listTables) {
    $t = $tables | Where-Object { $_.Name -eq $table }
    $p = $pageInfo[$table]
    $tpl = @{ projectId = $projId; name = "$($t.Label)管理模板"; code = "T_${table}_List"; templateType = 'List'; filterPageId = $p.Fil; summaryPageId = $p.Sum; detailPageId = $p.Det; renderView = 'RouteList'; isEnabled = $true }
    $r = Invoke-Post "/api/dynproject/template/save" $tpl
    Write-Output "模板 T_${table}_List Id=$($r.data.Id)"
}

# 学生管理：RouteCrud 模板（DynCrudPage 组件渲染，演示设计器组件驱动）
$sp = $pageInfo['Student']
$crudTpl = @{ projectId = $projId; name = '学生管理模板(Crud组件)'; code = 'T_Student_Crud'; templateType = 'List'; filterPageId = $sp.Fil; summaryPageId = $sp.Sum; detailPageId = $sp.Det; renderView = 'RouteCrud'; isEnabled = $true }
$r = Invoke-Post "/api/dynproject/template/save" $crudTpl
$crudTplId = $r.data.Id
Write-Output "模板 T_Student_Crud Id=$crudTplId"

# 班级管理：RouteTreeList 模板（左树右列表：年级树 → 班级列表）
$cp = $pageInfo['Class']
$treeTpl = @{ projectId = $projId; name = '班级管理模板(左树右列表)'; code = 'T_Class_TreeList'; templateType = 'List'; filterPageId = $cp.Fil; summaryPageId = $cp.Sum; detailPageId = $cp.Det; renderView = 'RouteTreeList'; isEnabled = $true }
$r = Invoke-Post "/api/dynproject/template/save" $treeTpl
$treeTplId = $r.data.Id
Write-Output "模板 T_Class_TreeList Id=$treeTplId"

# 主页模板（Home，展示学生汇总屏为数据看板）
$homeTpl = @{ projectId = $projId; name = '学校管理主页模板'; code = 'T_School_Home'; templateType = 'Home'; summaryPageId = $sp.Sum; renderView = 'RouteHome'; isEnabled = $true }
$r = Invoke-Post "/api/dynproject/template/save" $homeTpl
$homeTplId = $r.data.Id
Write-Output "模板 T_School_Home Id=$homeTplId"

# ============ 3. 网页路由 ============
function Save-WebPage([string]$route, [string]$name, [string]$title, [int]$tplId, [bool]$isHome, [int]$sort) {
    $wp = @{ projectId = $projId; route = $route; name = $name; title = $title; templateId = $tplId; isHome = $isHome; isEnabled = $true; sortOrder = $sort }
    $r = Invoke-Post "/api/dynproject/webpage/save" $wp
    Write-Output "路由 $route Id=$($r.data.Id)"
}

Save-WebPage '/home' '学校管理主页' '学校管理' $homeTplId $true 0
$sort = 1
foreach ($table in $listTables) {
    $t = $tables | Where-Object { $_.Name -eq $table }
    $tplList = (Invoke-Get "/api/dynproject/$projId/templates").data | Where-Object { $_.code -eq "T_${table}_List" }
    Save-WebPage $t.Route "$($t.Label)管理" "$($t.Label)管理" $tplList.Id $false $sort
    $sort++
}
Save-WebPage '/student-crud' '学生管理(组件)' '学生管理' $crudTplId $false $sort; $sort++
Save-WebPage '/class-tree' '班级管理(树)' '班级管理' $treeTplId $false $sort

Write-Output "===== 页面/模板/路由配置完成 ====="
