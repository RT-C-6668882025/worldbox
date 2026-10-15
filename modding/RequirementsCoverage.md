# 本轮缺口核查与实现对应

| 缺口 | 实现位置 | 核验 |
| --- | --- | --- |
| 普通兵额/岗位/退伍/出征不一致 | NativeLifecycle.Target/MilitaryJobs/CanRecruit/CanRemain/CanDeploy，Patch | 真实 DLL 编译、IL 调用检查、十成人叛军场景 |
| 叛军识别成原政权 | RebellionRules、NativeLifecycle.WarCreated | 核对原 APK startRebellion→startWar→War.newWar；进攻/防守方场景 |
| 军饷只支付抽样居民 | NativeLifecycle.StartYear/Step、TickCity | 130 人、64 人批次、全员支付、同年重放不重复 |
| 欠薪没有记录和补发 | RecordWork、Step、FundNativeIncome | 无资金产生债务；注资后仅补发一次；钱包满额不伪造债务 |
| 没钱不形成国家危机 | EconomyModel.Tick、FinishYear、Loyalty | 失修、持续压力、忠诚下降与恢复场景；原生叛乱调用编译 |
| 国库只是全国城市合计 | NationalOperation/NationalYear/NationalGrant | 独立发行、拨款、上缴、不足时不产生部分转账 |
| 个人不能操作双币 | NativeEconomyEditors、IssuePersonal/ExchangePersonal | 发行、回收、额度、兑换守恒 |
| 数量/汇率只有固定档位 | 原生行内 InputField | 真实 Unity DLL 编译；实机输入尚待验收 |
| 抽象产出吞原料、不产生货物 | EconomyModel、Delivered、Produced | 无实际产出时不凭空扣原料；真实产出只统计一次 |
| 企业没有扩张和招聘联系 | firmCapacity、MilitaryJobs、Manufacture、行业就业记录 | 投资与实际岗位、实际成人加工工资的资金守恒 |
| 工业/电力只是时代名称 | Civilization、EraReady、IndustrialTrade | 原材料→中间品、设施门槛、无资金不投产、工业品交易 |
| 军备没有后期生产和弹药 | InitWeapons/ModernEquipment/EquipmentMade/AmmoReady/Shot | IL 注入、零件消耗、失败攻击不扣弹、成功扣弹及缺弹停火 |
| 现代建筑没有外观 | IndustrialAppearance、Building.calculateMainSprite | 六种离线渲染、原生尺寸支点、染色读取、废墟回退 |
| 个人能力与原生特质重复 | NativeProgression.Ability/CompletedWork、GodCapacity、ShowUnit | 旧技能XP不再读取/增长；直接读原生属性；移除额外五行和升级奖励 |
| 资金统计易混淆/溢出 | 原生汇总饱和加法、完整普查及分类统计 | 整数溢出边界与 130 人完整持币普查 |
| 全装备与个人特质开放 | NativeUnlocks、原生 availability/load/pool 注入 | 解锁集合、编辑标志、装备池、幂等及不伪造成就测试 |
| 发展全部使用金币 | 去除模型、城市、个人、国库的自动 GOD 兑换与 GOD 工资 | 缺金币但有 GOD 时拒付；显式手动兑换后可支付；编译后调用路径核验 |
| 全动物、生物、怪物词条 | NativeUnlocks、ActorAssetLibrary/SubspeciesTraitLibrary post_init | 生物图鉴、接收/编辑/检查开关、亚种记录、船只/模板排除及原生编译验证；实机未测 |
| 九类组织编辑与动态实力加成 | NativeOrganizations、NativeOrganizationEditor、原生统计窗/MapBox.updateFinish | 排名转移、分类效果、上限、关闭、存档字段、世界重置、活税率；原生接口与IL检查 |

这张表中的“编译/离线/替身场景”不等于 Android 实机通过。企业是行业容量模型；科技保留原生文化、阅读和人物成长，新增产业与设备门槛。没有另造独立交通 AI、企业法人体系、完整现代车辆素材库或任意尺寸地图编辑器。
