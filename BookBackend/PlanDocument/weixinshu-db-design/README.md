# 心书 (weixinshu.com) 数据库设计方案

> **声明**: 本设计完全基于 weixinshu.com 公开网页内容推断，**不是**对其真实生产数据库的还原。仅供学习与参考。

## 1. 业务概览

心书是一个"社交内容导入 → 作品编辑排版 → 定制印刷下单 → 支付发货 → 晒单传播 → 代理分销"的平台。核心业务链路有两条：

1. **做书主链路**: 用户登录 → 授权微信/微博 → 导入朋友圈/微博内容(或站内补写) → 编辑排版 → 选择封面/版式/装帧 → 下单支付 → 印刷发货 → 收货晒单
2. **代理结算链路**: 代理申请 → 审核通过 → 设置品牌/定价 → 生成专属链接 → 客户归因 → 下单成交 → 佣金结算 → 提现

## 2. 业务域划分 (8 个域)

| 域 | 表前缀 | 核心职责 | 表数量 |
|---|---|---|---|
| 身份认证 | `identity_` | 用户主账号、画像、微信/微博身份绑定、登录会话、刷新令牌 | 5 |
| 内容导入 | `content_` | 外部平台内容采集、站内上传、标准化图文条目与媒体 | 6 |
| 作品编辑 | `book_` | 作品项目、内容条目、素材、封面、序言、版式、版本快照 | 7 |
| 商品目录 | `catalog_` | 分类、SPU、SKU、规格、封面模板、版式模板、价格规则、套餐 | 10 |
| 交易履约 | `order_` | 订单、订单项、价格快照、支付、支付交易、收货地址、物流、物流事件 | 8 |
| 社区晒单 | `community_` | 晒单帖子、媒体、标签、等级 | 4 |
| 代理分销 | `agent_` | 申请、账号、品牌、专属链接、定价、归因、佣金流水、提现 | 8 |
| 合规审计 | `sys_` | 隐私协议版本、授权记录、审计日志、操作日志、第三方共享记录 | 5 |

**合计: 53 张表**

## 3. 核心设计策略

### 3.1 身份与第三方分离
- `identity_user` 是站内唯一主账号
- 微信/微博等外部身份通过 `identity_external_identity` 多对一绑定
- 扫码登录/授权流程通过 `identity_auth_session` 管理中间态

### 3.2 导入内容与编辑结果分离
- `content_post` 保存从朋友圈/微博导入的原始标准化内容
- `book_project_entry` 保存进入成书后的编辑结果，通过 FK 引用原始内容
- 站内手动补写内容直接存在 `book_project_entry.custom_text`

### 3.3 订单快照化
- `order_price_snapshot` 在下单时冻结完整商品信息：产品类型、尺寸、装帧、版式、封面、页数、基础价、页单价、代理加价、折扣、最终价
- `book_project_version` 在下单时冻结完整排版快照
- 历史订单可独立复原，不依赖当前商品目录配置

### 3.4 代理归因独立
- 代理体系通过 `agent_customer_attribution` 记录用户与代理的绑定关系
- 佣金通过 `agent_commission_ledger` 独立记账，支持结佣、冲正、冻结、提现
- 代理可自定义品牌名称和商品加价

## 4. 核心状态机

### 4.1 作品状态 (`book_project.status`)
```
0=草稿 → 1=编辑中 → 2=待下单 → 3=已下单 → 4=已归档
```

### 4.2 订单状态 (`order_order.status`)
```
0=待支付 → 1=已支付 → 2=生产中 → 3=待发货 → 4=已发货 → 5=已签收 → 6=已完成
                                                                    ↘ 7=已关闭
         ↘ 8=已取消
```

### 4.3 支付状态 (`order_payment.status`)
```
0=待支付 → 1=支付中 → 2=已支付
                     ↘ 3=支付失败
         2=已支付 → 4=已退款 / 5=部分退款
```

### 4.4 物流状态 (`order_shipment.status`)
```
0=待发货 → 1=已发货 → 2=运输中 → 3=已签收
                               ↘ 4=异常
```

### 4.5 代理申请状态 (`agent_application.status`)
```
0=待审核 → 1=已通过 / 2=已拒绝
1=已通过 → 3=已注销
```

### 4.6 佣金交易类型 (`agent_commission_ledger.txn_type`)
```
earn — 订单结佣
reversal — 退款冲正
freeze — 提现冻结
unfreeze — 提现取消解冻
withdraw — 提现扣减
```

### 4.7 提现状态 (`agent_withdraw_request.status`)
```
0=待审核 → 1=处理中 → 2=已到账
                     ↘ 3=已拒绝
```

## 5. 关键约束与索引

### 5.1 唯一约束
| 表 | 唯一键 |
|---|---|
| `identity_user` | `user_no`, `phone` |
| `identity_external_identity` | `(platform, external_uid)` |
| `order_order` | `order_no` |
| `order_payment` | `payment_no` |
| `order_payment_txn` | `idempotency_key` |
| `order_shipment` | `shipment_no` |
| `agent_account` | `agent_code` |
| `agent_share_link` | `link_code` |
| `community_unboxing_post` | `post_no` |
| `content_post` | `(user_id, source_platform, source_post_id)` |
| `book_project_version` | `(project_id, version_no)` |

### 5.2 高频查询索引
| 查询场景 | 索引 |
|---|---|
| 用户作品列表 | `book_project(user_id, updated_at DESC)` |
| 用户订单列表 | `order_order(user_id, created_at DESC)` |
| 订单状态筛选 | `order_order(user_id, status)` |
| 物流单号回查 | `order_shipment(shipment_no)` — UK |
| 晒单时间线 | `community_unboxing_post(status, published_at DESC)` |
| 晒单按等级 | `community_unboxing_post(level_id, published_at DESC)` |
| 代理佣金汇总 | `agent_commission_ledger(agent_id, created_at DESC)` |
| 内容导入帖子 | `content_post(user_id, posted_at DESC)` |

## 6. 冷热分层策略

- **热数据**: 用户信息、活跃作品、近期订单、佣金余额
- **温数据**: 历史订单、已归档作品、晒单内容
- **冷数据**: 导入原始内容、物流事件、支付回调原文、审计日志
- **对象存储**: 图片、视频、封面源文件、晒单大图 — 数据库只存 URL、哈希和尺寸

## 7. 商品价格计算公式

基于产品页信息推断的定价逻辑:

```
最终单价 = 基础价格 + 每页单价 × 页数 + 代理加价 - 折扣
```

示例 (微信书 A5 经济装, 60页):
```
基础价格: ¥18.00
每页单价: ¥0.70
页数: 60
计算价: 18 + 0.70 × 60 = ¥60.00
```

套餐价格直接使用 `catalog_bundle.bundle_price`, 不走页数计算。

## 8. 文件清单

| 文件 | 说明 |
|---|---|
| [sql/01_identity.sql](sql/01_identity.sql) | 身份域 DDL (5 表) |
| [sql/02_content.sql](sql/02_content.sql) | 内容导入域 DDL (6 表) |
| [sql/03_book.sql](sql/03_book.sql) | 作品域 DDL (7 表) |
| [sql/04_catalog.sql](sql/04_catalog.sql) | 商品目录域 DDL (10 表) |
| [sql/05_order.sql](sql/05_order.sql) | 交易履约域 DDL (8 表) |
| [sql/06_community.sql](sql/06_community.sql) | 社区晒单域 DDL (4 表) |
| [sql/07_agent.sql](sql/07_agent.sql) | 代理分销域 DDL (8 表) |
| [sql/08_sys.sql](sql/08_sys.sql) | 合规审计域 DDL (5 表) |
| [er-diagram.mmd](er-diagram.mmd) | Mermaid ER 图源文件 |

## 9. 两条关键业务链路

### 链路 A: 社交内容导入到成书下单

```
identity_user
  → identity_external_identity (微信授权)
    → content_source_account (绑定内容源)
      → content_import_job (发起导入)
        → content_post + content_post_media (标准化存储)
          → book_project (创建作品)
            → book_project_entry (引用导入内容 + 补写)
            → book_project_cover (选封面)
            → book_project_layout (选版式/尺寸/装帧)
            → book_project_preface (写序言)
            → book_project_version (冻结快照)
              → order_order + order_order_item (下单)
                → order_price_snapshot (价格快照)
                → order_payment → order_payment_txn (支付)
                → order_shipment → order_shipment_event (发货)
```

### 链路 B: 代理推广归因到佣金结算

```
identity_user (代理)
  → agent_application (申请)
    → agent_account (审核通过)
      → agent_brand (自定义品牌)
      → agent_share_link (生成推广链接)
      → agent_pricing_rule (自定义加价)

identity_user (客户)
  → agent_customer_attribution (点击代理链接, 归因)
    → order_order.agent_account_id (下单时携带代理标记)
      → agent_commission_ledger (earn: 订单结佣)
        → agent_withdraw_request (代理提现)
          → agent_commission_ledger (freeze → withdraw)
```

## 10. 已知假设与证据来源

| 假设 | 证据来源 |
|---|---|
| 登录方式为微信扫码 + 可选账密 | 登录页 `/signin` — 微信扫码为主, 有切换账密按钮 |
| 支持微信/微博两个外部平台导入 | 首页与产品页 — 明确展示微信书、微博书为独立产品 |
| 站内支持手动上传和补写 | 产品页 — "自定义封面/书名/内容", 助手页 — "导出私密内容" |
| 2种尺寸 × 多种版式 × 4种装帧 | 微信书产品页 — "2种尺寸 \| 6种版式 \| 3种装帧" (实际页面显示4种装帧选项) |
| 价格 = 基础价 + 页单价 × 页数 | 微信书产品页 — 明确展示公式与示例 |
| 支持套餐组合售卖 | 套餐页 — 6件套、3件套等含价格明细 |
| 晒单分白银/黄金/钻石等级 | 晒单列表页 — 等级标签与头像徽章 |
| 代理可自定义品牌和价格 | 代理介绍页 — 明确说明6大代理优势 |
| 两种代理结算方式 | 代理页 — 第三方结算和心书结算 |
| 支付宝和微信支付 | 页脚认证标识 |
| 收货信息受隐私协议约束 | 隐私协议页 — 明确收集收货人姓名/电话/地址 |

## 11. 低置信/未建模模块

以下模块因公开页面证据不足，未纳入本次设计：

- **后台 CMS**: 首页轮播、富文本页面管理
- **客服工单**: 联系客服入口存在，但具体工单系统结构未知
- **退款逆向流程**: 仅有支付状态中预留退款位，细节未公开
- **印厂排产系统**: 订单到发货之间的生产管控流程
- **站内消息/通知**: 公众号推送机制存在，站内消息盒子未知
- **财务总账**: 平台级收支与对账
- **优惠券/营销活动**: 页面显示有折扣概念但具体活动规则未公开
