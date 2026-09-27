# Third‑Party Notices
This document contains license information for third‑party open‑source libraries used in this project.

## Table of Contents
- [MIT License](#mit‑license)
- [Apache License 2.0](#apache-license-20)
- [Component List: Backend NuGet Packages](#component-list-backend-nuget-packages)
- [Component List: Frontend Static Libraries (wwwroot/lib)](#component-list-frontend-static-libraries-wwwrootlib)
- [Notes](#notes)

## MIT License
> The MIT License
>
> Permission is hereby granted, free of charge, to any person obtaining a copy
> of this software and associated documentation files (the "Software"), to deal
> in the Software without restriction, including without limitation the rights
> to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
> copies of the Software, and to permit persons to whom the Software is
> furnished to do so, subject to the following conditions:
>
> The above copyright notice and this permission notice shall be included in all
> copies or substantial portions of the Software.
>
> THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
> IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
> FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
> AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
> LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
> OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
> SOFTWARE.

## Apache License 2.0
> Applied to components marked **Apache‑2.0** below.
>
> When distributing this project (binaries / packages), you must:
> 1. Keep the original copyright notice and this license notice;
> 2. Include a copy of the full Apache License 2.0 text alongside the distribution
>    (the standard text is available at https://www.apache.org/licenses/LICENSE-2.0);
> 3. Preserve any NOTICE file of the upstream project if present.
>
> Apache License 2.0 is a permissive license: commercial use, closed‑source use,
> modification and redistribution are allowed. It does NOT require you to open‑source
> your own code.

## Component List: Backend NuGet Packages

### Shapeless
- Project Url: https://gitee.com/dotnetchina/Shapeless
- License: MIT
- Copyright (c) 百小僧 and contributors

### Newtonsoft.Json (Json.NET)
- Project Url: https://github.com/JamesNK/Newtonsoft.Json
- License: MIT
- Copyright (c) James Newton‑King

### Microsoft .NET / ASP.NET Core / Entity Framework Core
- Project Url: https://github.com/dotnet
- License: MIT
- Copyright (c) Microsoft Corporation

### SQLitePCL (Microsoft.Data.Sqlite dependency)
- Project Url: https://github.com/ericsink/SQLitePCL.raw
- License: MIT / Public‑Domain
- Copyright (c) Eric Sink

### SqlSugarCore
- Project Url: https://github.com/DotNetNext/SqlSugar
- License: Apache‑2.0
- Copyright (c) DotNetNext

## Component List: Frontend Static Libraries (wwwroot/lib)

> Files under `wwwroot/lib` are local copies of the following open‑source libraries.
> When distributing this project, this NOTICES file must travel with them.

### Vue.js / Vue Router
- Location: `wwwroot/lib/vue.global.prod.js`, `vue-router.global.prod.js`
- Project Url: https://github.com/vuejs/core , https://github.com/vuejs/router
- License: MIT
- Copyright (c) 2013‑present Yuxi (Evan) You

### Element Plus (UI components + icons)
- Location: `wwwroot/lib/element-plus/`
- Project Url: https://github.com/element-plus/element-plus
- License: MIT
- Copyright (c) Element‑Plus contributors

### ECharts
- Location: `wwwroot/lib/echarts.min.js`
- Project Url: https://github.com/apache/echarts
- License: **Apache‑2.0**
- Copyright (c) 2017 The Apache Software Foundation / Baidu Inc.

### CodeMirror
- Location: `wwwroot/lib/codemirror/`
- Project Url: https://github.com/codemirror/codemirror5
- License: MIT
- Copyright (C) 2017 by Marijn Haverbeke and others

### layui‑v2 (including layer dialog)
- Location: `wwwroot/lib/layui/`
- Project Url: https://gitee.com/layui/layui
- License: MIT
- Copyright (c) layui contributors

### jQuery
- Location: `wwwroot/lib/jquery/`
- Project Url: https://github.com/jquery/jquery
- License: MIT
- Copyright OpenJS Foundation and other contributors

### Axios
- Location: `wwwroot/lib/axios.min.js`
- Project Url: https://github.com/axios/axios
- License: MIT
- Copyright (c) 2014‑present Matt Zabriskie

### Lodash
- Location: `wwwroot/lib/lodash.min.js`
- Project Url: https://github.com/lodash/lodash
- License: MIT
- Copyright OpenJS Foundation and other contributors

### markdown‑it
- Location: `wwwroot/lib/markdown-it.min.js`
- Project Url: https://github.com/markdown-it/markdown-it
- License: MIT
- Copyright (c) 2014 Vitaly Puzrin

### SortableJS
- Location: `wwwroot/lib/sortable.min.js`
- Project Url: https://github.com/SortableJS/Sortable
- License: MIT
- Copyright (c) 2013‑2020 All contributors to Sortable

### Tailwind CSS
- Location: `wwwroot/lib/tailwind.css`
- Project Url: https://github.com/tailwindlabs/tailwindcss
- License: MIT
- Copyright (c) Tailwind Labs, Inc.

### Vue Draggable Plus
- Location: `wwwroot/lib/vue-draggable-plus.umd.js`, `vue-draggable-plus.umd.min.js`
- Project Url: https://github.com/Alfred-Skyblue/vue-draggable-plus
- License: MIT
- Copyright (c) 2023 Alfred‑Skyblue

## Notes
1. layui‑v2 is open‑source MIT library. **layuiAdmin (commercial template) is NOT used in this project.**
2. **ECharts** is licensed under Apache‑2.0 (not MIT): when distributing, please include the Apache License 2.0 full text alongside this NOTICES file.
3. This list may not be exhaustive. Use a license scanning tool (`dotnet-license-generator`) to get the full NuGet dependency list before release.
4. MIT license provides no patent‑grant.
5. Files under `wwwroot/lib` do not bundle their own LICENSE files; this NOTICES file serves as the attribution record for those local copies.
