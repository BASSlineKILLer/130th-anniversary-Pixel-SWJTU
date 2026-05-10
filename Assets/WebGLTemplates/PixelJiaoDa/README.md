# PixelJiaoDa WebGL Template

专为《我和我的像素交大》优化的 Unity WebGL 模板。

## 特性

1. **高分屏适配**：限制 `devicePixelRatio` 最大为 1.25，避免 4K/Retina 屏幕下 WebGL 渲染压力过大导致的卡顿
2. **音频后台处理**：监听页面 `visibilitychange` / `focus` / `blur` 事件，切后台时自动暂停 Unity 音频，返回时恢复
3. **自定义 Canvas 尺寸**：关闭 Unity 的自动 DPR 同步，手动控制 canvas backing store 大小

## 使用方法

1. 在 Unity 中打开 **File > Build Settings...**
2. 选择 **WebGL** 平台
3. 在 **Player Settings** 中找到 **Resolution and Presentation**
4. 在 **WebGL Template** 下拉菜单中选择 **PixelJiaoDa**
5. 执行 Build 或 Build And Run

## 与之前手动修改的区别

- 之前：每次 Build 后需手动修改 `web/web-demo/index.html`
- 现在：使用此模板，DPR 限制和音频处理逻辑会自动包含在每次 Build 中

## 技术细节

- 模板路径：`Assets/WebGLTemplates/PixelJiaoDa/`
- 主要文件：`index.html`（包含 DPR 限制、事件监听、Unity 加载逻辑）
- 占位文件：`thumbnail.png`（Build Settings 中显示的预览图，建议替换为项目 Logo）

## 注意事项

- 此模板假设项目中存在 `WebGLRuntimeBridge` MonoBehaviour 来处理页面可见性回调
- 如果更换模板或重置 Build 目录，这些优化会丢失，请确保使用此模板进行 WebGL Build
