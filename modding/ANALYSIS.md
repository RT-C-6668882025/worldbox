# 基础与解锁路径

基础包取自本次用户附件，哈希与 apk-upload-20261014/worldbox.apk 一致。该包已经是 Mono 转换包，并非官方原始 IL2CPP 包；真实程序集位于 assets/Mono/Managed.zip/Assembly-CSharp.dll。global-metadata.dat 是非元数据占位文本，不能以 IL2CPP 地址方式修改。

| 内容 | 真实入口 | 本次处理 |
| --- | --- | --- |
| 生物、动物、怪物、特殊单位特质 | BaseUnlockableAsset.isAvailable/isUnlocked/isUnlockedByPlayer；ActorAsset.can_edit_traits/can_receive_traits | 有针对性的放行；库链接后开放编辑；保留船只能力限制 |
| 亚种、王国、氏族、文化、宗教、语言特质 | 相同基类；BaseAugmentationAsset.can_be_given/can_be_removed；AugmentationsEditor.editorButtonClick | 开放添加删除和原生点击入口 |
| 基因 | GeneEditor.clickGeneAssetAction、LocusElement.OnDrop | 解除这两处编辑门槛；保留位点和染色体合法性处理 |
| 表型 | PhenotypeAsset、phenotype_library；物种／亚种表型关联 | 放行已有定义可用性；保留物种关联与原生展示方式，不凭空添加独立表型编辑器 |
| 武器、防具、饰品、物品、材料变体 | ItemAsset、EquipmentAsset、ItemLibrary.fillUnlockedPools | 放行已有定义；补齐 all → unlocked 候选池 |
| 装备效果 | ItemModAsset、mod_can_be_given | 开放原生效果编辑 |
| 品质 | ItemAsset.quality/rarity 与 ItemWindow.clickReforge → Item.reforge | 未发现独立解锁门槛，保留原生生成／重铸行为，不承诺新增任意品质选择功能 |

三个基类门槛仅对 ActorAsset 和非 WorldLawAsset 的 BaseAugmentationAsset 派生类返回解锁；不全局修改 Config.hasPremium。图标、布局、提示、编辑器和操作顺序沿用原包。内部隐藏条目继续受 show_in_meta_editor 过滤，避免把基础攻击模板等技术条目当成装备展示；不改变装备槽位与物种兼容条件。

生命周期：AssetManager.initLibs 完成所有库的 post_init 与 linkAssets 后调用一次 AssetsReady。返回指令本身改成 hook，保证原有 leave 到返回点的路径不会跳过 hook。ItemLibrary.fillUnlockedPools 每次完成后合并已有全部池，避免载入进度后重新锁回。补丁不依赖 GameProgress 已存在，不修改用户存档集合。

# 验证边界

Verify.cs 比较原程序集全部方法（包含嵌套类型），只允许 8 个指定方法改变，并检查修改处和辅助程序集的 IL 栈、5 个注入调用的签名和真实字段/方法引用。Smoke.cs 在 .NET 中执行修改后的真实游戏类型，不使用替身：13 类各四个门槛检查，加空对象、世界法则排除、动物编辑、船只保留、装备效果及空池入口，共 58 项。

构建进一步验证压缩包、签名，并逐文件比较除签名之外的 APK 载荷，仅允许 Managed.zip 改变。Unity 引擎、原生 so、AndroidManifest、资源均未修改。

当前环境无 adb、Android SDK、可用设备或 /dev/kvm，无法完成 Android 启动测试。发布必须为 prerelease，不得据静态检查宣称启动成功或稳定。

# 复现

Python 3.12+、Java 17、keytool、curl、Linux x64：

```bash
python3 modding/fetch_tools.py tools
python3 modding/build.py --apk /absolute/path/worldbox.apk --tools tools --output build
```

工具版本与下载哈希固定在 toolchain.json。APK 输入哈希不符即终止。测试签名私钥只留在构建输出，不进入源码或 Release。各独立构建的测试签名可能不同。
