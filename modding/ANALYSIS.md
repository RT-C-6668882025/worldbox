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

Verify.cs 比较原程序集全部方法（包含嵌套类型），只允许 12 个指定方法改变，并检查修改处和辅助程序集的 IL 栈、7 个注入调用的签名和真实字段/方法引用。Smoke.cs 在 .NET 中执行修改后的真实游戏类型，不使用替身：13 类各四个门槛检查，加空对象、世界法则排除、动物编辑、船只保留、装备效果及空池入口，原有 58 项，加 27 项军队上限检查，共 85 项。

构建进一步验证压缩包、签名，并逐文件比较除签名之外的 APK 载荷，仅允许 Managed.zip 和明确修正过的 AndroidManifest.xml 改变。Unity 引擎、原生 so、资源均未修改；AndroidManifest 仅修正安装位置与调试标志。

已运行 ADB 设备枚举，未发现连接的 Android 设备；当前也没有可用 Android 模拟器或 /dev/kvm，无法完成 Android 启动测试。发布必须为 prerelease，不得据静态检查宣称启动成功或稳定。

# 复现

Python 3.12+、Java 17、keytool、curl、Linux x64：

```bash
python3 modding/fetch_tools.py tools
python3 modding/build.py --apk /absolute/path/worldbox.apk --tools tools --output build
```

工具版本与下载哈希固定在 toolchain.json。APK 输入哈希不符即终止。测试签名私钥只留在构建输出，不进入源码或 Release。各独立构建的测试签名可能不同。

# 军队人数限制

原链路：CityBehCheckCitizenTasks.getPossibleWarriors 将成年人口乘 City.getArmyMaxMultiplier（物种基础比例＋领袖加成），经职业分配写入 CityStatus.warrior_slots。City.isArmyFull 阻止招募，City.isArmyOverLimit 触发超额退役，City.getMaxWarriors 为界面／王国统计提供上限。

新增四处修改：getPossibleWarriors 和 getMaxWarriors 调用 ArmyCapacity，返回非负成年人口；满员、超额两判断返回 false。避免 int.MaxValue 伪无限容量引起王国汇总溢出。候选数量仍受现有人口约束，不直接生成或强制转职。世界法则开关、粮食、幼年、文化资格、原生职业分配和招募计时保持原样。粮食不足仍可能导致退役。

军队检查执行真实 City 和 CityBehCheckCitizenTasks 类型：空城市、未初始化状态、0/1/16/100/10000/int.MaxValue 成年人口的两处容量与两处门槛、负人口暂态，共新增 27 项。

# 安装兼容排查

截图为系统安装器 `packageInfo is null (33)`，发生在游戏启动前。旧包的 Manifest 可以被 Androguard 解析，v1/v2/v3 签名均有效；resources.arsc 原已不压缩且4字节对齐。因此这些结果不足以证明设备一定可安装。

发现基础包 installLocation 为2（Android 枚举 preferExternal，1才是 internalOnly），debuggable 为true。本次将二者归零，使用 auto 安装位置并关闭调试；Manifest 同时改为 ZIP_STORED 并4字节对齐。install_compat.py 严格验证源值，只修改这两个32位值。check_install.py 校验完整二进制清单与预期输出一致、SDK与native库解压属性保持不变、清单和资源不压缩对齐、无重复ZIP条目。CI额外使用 Android SDK aapt2 解析包名、资源和启动Activity。

这是针对安装配置的兼容修复，不能仅凭截图证明根因，仍须设备安装回测。保留原解锁与军队补丁。
