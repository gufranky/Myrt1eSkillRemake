# 静态导航网格与安全随机传送

`NavMeshService` 不使用 CS2 内存特征码。地图开始时，它按以下顺序寻找导航数据：

1. `game/csgo/maps/<map>.nav`
2. `game/csgo/maps/<map>.vpk` 内的 `.nav` 条目
3. `game/csgo/maps/workshop/**/<map>.nav`
4. `game/csgo/maps/workshop/**/<map>.vpk` 内的 `.nav` 条目

NAV/VPK 解析由 MIT 许可的
[ValveResourceFormat](https://github.com/ValveResourceFormat/ValveResourceFormat)
和 [ValvePak](https://github.com/ValveResourceFormat/ValvePak) 完成。Release 构建只复制
NAV 解析所需的运行库，不包含无关的渲染器及多平台原生资源。

## 落点选择

服务会选择尺寸最接近 CS2 玩家碰撞体的 NAV hull，查找玩家当前区域，再沿
`NavMeshArea.Connections` 做 BFS。候选点只会从该连通分量中抽取，因此不会传送到
与玩家当前位置隔绝的导航岛。

导航网格只代表地图生成时的静态可行走区域。每个候选点仍须经过 RayTraceAPI：

- 从候选点上方向下追踪真实地面；
- 拒绝坡度过大的表面；
- 用 `32 x 32 x 72` 的玩家 hull 检查完整占用空间；
- 拒绝距离其他存活玩家小于 64 单位的位置；
- 传送后下一帧再次检查，失败时回到原位置。

缺少 NAV、NAV 解析失败或 RayTraceAPI 不可用时，服务会失败关闭，不执行传送。

## 测试命令

```text
css_nav_status
css_nav_randomtp
```

`css_nav_status` 显示地图名、NAV 区域数、数据来源及最近错误。
`css_nav_randomtp` 需要 `@css/generic` 权限，只用于管理员在测试服务器验证落点。

目前这是框架服务和调试入口，尚未绑定为正式技能或娱乐事件。
