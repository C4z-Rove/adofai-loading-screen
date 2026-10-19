Adofai Loading Screen  ——  冰与火之舞 (A Dance of Fire and Ice) Mod
====================================================================

加载动画（2.4.0 新增）
----------------------
内置 7 种样式，UMM 面板「加载画面 · 动画样式」里点一下切换：

    sweep    红/蓝色块从左扫到右（默认）
    pulse    整条按进度填充，颜色和亮度呼吸
    wave     整条按进度填充，顶边做正弦波浪
    flow     斜纹流动的进度条
    spinner  屏幕中央一个旋转圆环（不显示顶栏）
    dots     屏幕中央三个点依次亮起
    custom   使用自己放的动画素材

中央指示器（spinner / dots / custom）可调大小 AnimScale、垂直偏移 AnimOffsetY；
custom 也可以放到顶栏：AnimSlot = bar。

boss 动画（魔王曲风格）
------------------------
  UMM 里把动画样式切到 oss，进关卡时就是这段演出（总长约 4.6 秒）：

    1. 黑屏淡入
    2. 评级圆圈从中心放大到屏幕中央（只显示字母，数字已经抠掉）
    3. 数字从屏幕下方飞进圆圈里：十位先起飞、个位后起飞，前快后缓
    4. 中段夹带故障特效（圆圈抖动 + 数字红蓝分离残影）
    5. 整体淡出，直接接谱面

  数字是用文字框画的，不是图片，所以任何数字都能显示。
  圆圈用的是 Marks\_split\<评级>_L.png（已去掉数字的版本），
  由 tools\split_marks.py 从 Marks\ 的图标批量生成；
  没有对应 _L 图时会退回整张图标（此时不显示飞入的数字）。

  时长固定 4.6 秒（即使关卡加载更快也会等它演完）。

自定义动画
----------
目录：Mods\PhigrosLoading\Animations\<名字>\
UMM 面板里点按钮切换文件夹，「打开目录」直接打开它。两种格式：

  1) PNG 序列
       Animations\my\0001.png 0002.png ...
       Animations\my\anim.json  { "fps": 24 }

  2) 雪碧图
       Animations\my\sheet.png
       Animations\my\anim.json  { "columns": 8, "frames": 48, "fps": 24 }

限制：最多 128 帧，单帧建议 ≤1024px；解码后缓存在内存，切帧不读盘。
帧率优先级：AnimFps(>0) > anim.json 的 fps > 24。
自带一个示例：Animations\demo_spin\（24 帧旋转圆环）。
功能
----
1) 加载画面
   把「选关 → 进入关卡」时的那段黑屏过渡，换成 Phigros 风格的加载画面：

     · 背景：关卡曲图（铺满、缓慢放大）
     · 顶部：Phigros 同款加载条（红/蓝色块交替从左扫到右）+ 右上角百分比
     · 右侧：曲名 / 曲师 / 谱师
     · 右下角：评级图标（来自 Marks\，未评级就是 Unranked）

   最短显示 1.2 秒（可调）；关卡本身加载更慢时不会额外延长，上限 90 秒防卡死。

2) 游戏内角落文字
   进关卡玩的时候：

     · 左下角：曲名
     · 右下角：TUF 评级 / Lv.GG 评级（来自 AutoRating）

   都贴在最角落，按屏幕高度等比缩放，任何分辨率/长宽比都不会跑偏。
   暂停、结算、编辑器、关卡加载中会自动隐藏。

3) 退出关卡时评级
   离开关卡时弹出评级窗：

     · 五个分类：Planetary / Galactic / Universal / UJ＆Q / 特殊
     · 右上角显示当前评级，AutoRating 有结果时给出建议并预选
     · 「保存」写入；「未评级」清空；「取消」不改
     · 已经评过级的谱面（SkipRatedPrompt）直接跳过，进退出动画
     · 评级写进 LevelLibrary 的 marks.txt（它加载着时），否则写 ratings.txt

4) 退出动画
   离开关卡也播一段类似进关的动画（曲图 + 信息、不带进度条、约 0.5 秒），
   替掉原版黑屏。配置项 ExitAnimation。

5) 选关界面徽章
   自定义关卡选关（CLS）的谱面详情里：

     · 「完成度 / 挑战次数」右上角：评级图标
     · 「Best XACC」那一行右边：通关判定图标（wide / normal / strict）
       大小 / 水平偏移 / 垂直偏移都能在 UMM 面板里调
       （ClsJudgeScale / ClsJudgeOffsetX / ClsJudgeOffsetY）

   判定：以「评级窗里手选」为准（默认）。
     退出关卡时会弹评级窗，左下角 Judge 一行选 宽判/标判/严判/无，保存即生效。
     配置项 RecordJudge 默认关；打开后通关时还会自动把当时的判定记下来。
     图标对应 Marks\wide.png / normal.png / strict.png。

   判定数据从哪来（重要）：
     游戏的「宽判/标判/严判」是**全局设置**（GCS.difficulty = Lenient/Normal/Strict），
     并不存在谱面文件里，所以「这张谱面通关时用的判定」只能由本 Mod 自己记录：
     每次通关（States.Won）时把当前判定写进 judges.txt，选关界面就会显示对应图标。
     右下角那个彩色靶心就是同一个值（绿=宽判、灰白=标判、红=严判），
     所以不需要截图取色，直接读这个值即可。

   判定也可以在评级窗口里直接选：窗口左下角「判定」四个按钮（宽判/标判/严判/无），
   保存后写进本 Mod 的 judges.txt，并同步给 LevelLibrary（如果它加载着）。
   这样不依赖 LevelLibrary 也能显示宽判。

   判定数据按这个顺序找：
     1. LevelLibrary 内存里的 MarkStore.GetJudge（它启用时就有）
     2. marks.txt 第三列（宽判 / 标判 / 严判，也接受 wide/normal/strict、0/1/2）
     0. 本 Mod 的 judges.txt（在评级窗口里选的，优先级最高）
     3. records.txt 里这张谱面「最佳通关」的判定槽（0=宽判 1=标判 2=严判）

   注意：这三样都来自 LevelLibrary。它被关掉时，我们只能读它已经落盘的文件；
   在它的界面里新设的判定要等它保存后才会出现在这里。

   两枚徽章都做了「稳定后淡入」：位置连续 0.6 秒没变化才淡入（约 0.2 秒），
   切换谱面 / 面板刷新时会先淡出，所以看不到图标瞬移。


评级图标从哪来
--------------
按这个顺序找（第一个有效的就用它）：

    1. config.json 里的 MarksPath（填了才用）
    2. Mods\PhigrosLoading\Marks\              ← 推荐放这里
    3. Mods\LevelLibrary\assets\marks\         ← 没放上面就用这个
    4. Mods\PhigrosLoading\Difficulties\       ← 旧兜底

图标是圆形评级图（P1、G5、U12、Unranked…），另有 wide / normal / strict
三张判定图。往对应文件夹里一丢就能用，不用改代码。

评级**数据**（谁定了多少级）和图标目录是分开的：
  · LevelLibrary 加载着 → 问它的 MarkStore（保存也同步到它列表）；
  · 没加载 → 读它的 marks.txt 兜底；
  · 再没有 → 用本 Mod 的 ratings.txt。


目录结构
--------
Mods\PhigrosLoading\
    Info.json
    PhigrosLoading.dll
    config.json          ← 配置，删掉会自动重建
    Marks\               ← 评级图标 / 判定图标（推荐放这里）
    Difficulties\        ← 旧版兜底难度图（可留空）
    Covers\              ← 自定义曲图（可选）
    ratings.txt          ← LevelLibrary 不在时的评级存档（自动生成）


自定义曲图（可选）
------------------
官方关卡没有曲图，自定义关卡一般会读它自己的 previewImage。
想自己指定就在 Covers\ 里放图片：

    Covers\1-1.png              ← 关卡名（官关卡）
    Covers\MyLevel.png          ← 谱面文件名（不含 .adofai）
    Covers\<谱面所在文件夹名>.png

优先级：Covers 自定义 > 关卡 previewImage > 关卡 bgImage > 世界 portal 图 > 渐变色


跟 AutoRating 联动
------------------
装了 AutoRating（Mods\AutoRating）并启用时，只读它已经算好的结果，不重复计算：

  · 右下角显示 `TUF评级 / Lv.GG评级`，例如 `P18 / Lv.18`、`U12 / Lv.22.2`
    （缺哪项就只显示另一项）；配置项 AutoRatingDisplay。
  · 评级弹窗里没手动评级时给建议并预选（AutoRating：P18（★9.62））。
  · 按「谱面路径 + 修改时间」缓存在 autorating_cache.json，
    下次进同一关的加载画面也能直接显示。

AutoRating 没装 / 没启用时自动忽略，一切照旧。


局内快捷键 / 跳过评级
--------------------
  · 快捷键（默认 F7，可在 UMM 里点按钮重新绑定）：暂停、未开始、结算时按下，
    直接弹评级窗改当前谱面的级；正在打时不响应。
  · 已评级 → 退出时跳过评级窗；没评级 → 先弹窗再过渡。


UMM 总开关（实时生效）
----------------------
在 UMM 的 Mods 列表里关掉本模组，所有元素立刻消失、hook 停止工作：
加载画面、角落文字、选关图标、评级弹窗都会马上收起；若弹窗正开着会自动关掉
并放行被拦住的切场景。重新打开后下一次进关/切场景自动恢复。


英文 / 中文本地化
------------------
  · 界面文案跟随游戏语言（中文→中文，其它→英文）。
  · 也可以在 UMM 面板最上面强制 自动 / 中文 / English（Language = auto/zh/en）。
  · 覆盖：UMM 面板、评级弹窗、加载画面固定文字、选关徽章提示；
    谱面数据（曲名/曲师/谱师内容）本身不翻译。


顺便修了个语言问题
------------------
  症状：设置里语言是英文，游戏里却一直是中文。
  原因：游戏启动时 RDString.Setup() 读语言的那一刻 PlayerPrefs 里的 language
        还没读到真实值（回退成系统语言=中文），等真实值读出来已经晚了。
        （抓过调用栈，触发者是游戏自己的 ADOStartup.SetupSystems，不是某个 mod；
          也扫过所有 mod，没有任何一个调用过语言相关 API。）
  修复：启动后按设置把语言重新应用一次，并持续监视——设置里改语言立刻生效。
        配置项 FixLanguage，关掉就恢复原样。


config.json
-----------
    Enabled             加载画面开关
    RatingPrompt        离开关卡时弹评级窗
    SkipRatedPrompt     已评级时跳过评级窗
    Hotkey              局内改评级快捷键（KeyCode 名字）
    FixLanguage         修复语言被锁成中文
    ExitAnimation       离开关卡也播动画
    InGameDifficulty    游戏内角落文字
    AutoRatingSuggest   评级弹窗采用 AutoRating 建议
    AutoRatingDisplay   右下角显示 AutoRating（TUF / Lv.GG）
    ClsBadge            选关界面显示评级/判定图标
    ClsBadgeOffsetX/Y   选关评级图标偏移（单位=图标边长）
    ClsJudgeScale       选关判定图标大小（倍率，基于行高）
    ClsJudgeOffsetX/Y   选关判定图标偏移（单位=图标边长）
    MinLoadingSeconds   加载画面最短时长
    MaxLoadingSeconds   加载画面最长时长（防卡死）
    Dim                 曲图压暗强度 0~1
    ShowPercent         加载条显示百分比
    KenBurns            曲图缓慢放大
    BarSweep            Phigros 加载条（红蓝交替扫过）；关掉=普通白色进度条
    BarSweepSeconds     扫过一次的时长
    BarSweepWidth       色块宽度（占整条比例）
    BarThickness        加载条高度
    PercentFont         百分比字号
    MarksPath           图标目录（留空=自动找）
    Language            auto / zh / en
    PreviewOnStart      调试：启动 2 秒后预览「角落文字→加载画面→评级窗」
    其余 TitleX/TitleY/TitleFont、RatingX/RatingY/RatingFont、
    InfoRight/InfoY/LoadTitleFont/LoadArtistFont/LoadChartFont、
    DiffHeight/DiffRight/DiffBottom 就是设置面板里的位置/字号，直接改文件也行。


注意事项
--------
· 加载是同步阻塞的，加载途中画面会「定住」一下，属正常现象。
· 评级弹窗打开时，游戏里的其它切场景触发会被暂时吞掉，选完才继续。
· 弹窗只支持鼠标点击（Esc 会开游戏自己的暂停菜单，所以没做快捷键）。


卸载
----
删除 Mods\PhigrosLoading 整个文件夹即可。

源码
----
源码与编译脚本在 default-workspace\adofai-phigros\（build.bat 用 Roslyn csc 编译）。
