# v1.2.1
* 修复存档时间戳显示问题
* Fixed save timestamp display issues

# v1.2.0
* 新增传送UI界面功能！按下 `Ctrl + ` ` 键（位于数字键1左边，ESC键下面，同时也是波浪号~键）打开可视化传送界面
* 支持无限存档槽，可在UI内随意存档、读档、删除、覆盖、传送椅子、重置坐标、安全重生等所有功能
* Added Teleport UI Interface! Press `Ctrl + ` ` key (located to the left of number key 1, below the ESC key, also the tilde ~ key) to open visual teleport interface
* Unlimited save slots support, all functions available in UI: save, load, delete, overwrite, bench teleport, reset coordinates, safe respawn, and more

# v1.1.3
* 修复椅子传送卡进地板的问题，传送到椅子时自动添加安全偏移，提升传送体验
* Fixed bench teleport stuck in ground issue, automatically applies safe offset when teleporting to bench, improved teleport experience

# v1.1.2
* 优化传送等待机制，提升稳定性，减少角色传送后在屏幕外、摄像头不跟随等传送BUG发生几率
* Optimized teleport waiting mechanism, improved stability, reduced occurrence probability of teleport bugs like hero out of screen or camera not following

# v1.1.1
* 优化了安全重生的功能
* Improved safe respawn functionality

# v1.1.0
* 新增安全状态检查功能，防止在角色死亡、坐椅子、重生时进行保存和传送操作，避免卡BUG
* Added safety state checks to prevent save and teleport operations during death, sitting on bench, or respawning to avoid bugs

# v1.0.6
* 修复手柄摇杆误触发问题，新增完整的手柄按键自定义功能，详见README按键映射表
* Fixed gamepad joystick false trigger issues, added complete gamepad key customization, see README key mapping table

# v1.0.5
* 新增紧急传送功能 (Alt+减号 或 LB+RB+X)，用于存档丢失导致困死场景的紧急救援
* 新增紧急返回主菜单功能 (Ctrl+F9)，解决角色失控（如飞起来并且无法呼出菜单只能强制关闭游戏）的问题
* 新增椅子传送功能 (Alt+7 或 LB+RB+B)，可直接传送到最后的重生点椅子
* Added emergency teleport function (Alt+minus or LB+RB+X) for emergency rescue when save data is lost causing stuck in scenes
* Added emergency return to main menu function (Ctrl+F9) to solve character out of control issues (such as flying and unable to open menu, forcing game closure)
* Added bench teleport function (Alt+7 or LB+RB+B) to directly teleport to last respawn point

# v1.0.41
* 优化README文档样式和结构
* Optimized README documentation style and structure

# v1.0.4
* 新增音效音量配置选项 (0.0-1.0，0为关闭)
* Added audio volume configuration option (0.0-1.0, 0 to disable)

# v1.0.3
* 新增存档音效反馈功能
* 新增彩蛋音效配置选项 (默认/特殊音效切换)
* Added save sound effect feedback
* Added easter egg audio configuration option (default/special sound toggle

# v1.0.2
* 修复暂停传送的BUG
* Fixed pause teleport bug

# v1.0.1
* 新增手柄支持功能
* 新增按键配置功能
* 新增持续化保存功能
* 新增安全重生到入口功能（循环）
* 新增重置所有坐标功能
* Added gamepad support
* Added key configuration
* Added persistent save functionality
* Added safe respawn to entry points (cycling)
* Added reset all coordinates functionality

# v1.0.0
* 初始版本
* Initial release
