<template>
  <n-modal
    v-model:show="visible"
    preset="card"
    :style="{ width: '1000px', height: '90vh' }"
    :title="isEdit ? '编辑防火墙规则' : '新建防火墙规则'"
    :closable="true"
    :auto-focus="false"
    :mask-closable="false"
    class="firewall-rule-modal"
    @close="handleClose"
  >
    <div class="rule-form-container">
      <n-form
        ref="formRef"
        :model="formData"
        :rules="formRules"
        label-placement="top"
        require-mark-placement="right-hanging"
        class="firewall-form"
      >
        <!-- 标签页 -->
        <n-tabs v-model:value="activeTab" type="line" size="medium" animated class="rule-tabs">
          <!-- 基本信息 -->
          <n-tab-pane name="basic" tab="基本信息">
            <div class="tab-content">
              <div class="form-section">
                <div class="section-title">
                  <n-icon :component="InformationCircleOutline" />
                  规则标识
                </div>
                <div class="form-grid">
                  <n-form-item path="name" label="规则名称" required>
                    <n-input
                      v-model:value="formData.name"
                      placeholder="输入唯一的规则名称"
                      maxlength="255"
                      show-count
                    />
                  </n-form-item>

                  <n-form-item path="description" label="规则描述">
                    <n-input
                      v-model:value="formData.description"
                      type="textarea"
                      placeholder="描述此规则的用途和目的"
                      :rows="3"
                      maxlength="500"
                      show-count
                    />
                  </n-form-item>

                  <n-form-item path="grouping" label="规则分组">
                    <n-input
                      v-model:value="formData.grouping"
                      placeholder="可选：输入分组名称"
                      maxlength="100"
                    />
                  </n-form-item>
                </div>
              </div>

              <div class="form-section">
                <div class="section-title">
                  <n-icon :component="SettingsOutline" />
                  规则行为
                </div>
                <div class="form-grid">
                  <div class="form-row">
                    <n-form-item path="enabled" label="规则状态">
                      <n-radio-group v-model:value="formData.enabled">
                        <n-radio :value="true">启用</n-radio>
                        <n-radio :value="false">禁用</n-radio>
                      </n-radio-group>
                    </n-form-item>

                    <n-form-item path="direction" label="规则方向" required>
                      <n-radio-group v-model:value="formData.direction">
                        <n-radio :value="1">入站 (Inbound)</n-radio>
                        <n-radio :value="2">出站 (Outbound)</n-radio>
                      </n-radio-group>
                    </n-form-item>
                  </div>

                  <n-form-item path="action" label="规则动作" required>
                    <n-radio-group v-model:value="formData.action">
                      <n-radio :value="1">允许 (Allow)</n-radio>
                      <n-radio :value="0">阻止 (Block)</n-radio>
                    </n-radio-group>
                  </n-form-item>
                </div>
              </div>
            </div>
          </n-tab-pane>

          <!-- 应用程序和服务 -->
          <n-tab-pane name="application" tab="应用程序">
            <div class="tab-content">
              <div class="form-section">
                <div class="section-title">
                  <n-icon :component="AppsOutline" />
                  应用程序路径
                </div>
                <div class="form-grid">
                  <n-form-item path="applicationName" label="应用程序">
                    <n-input
                      v-model:value="formData.applicationName"
                      placeholder="例: C:\Program Files\Example\app.exe (留空表示所有程序)"
                    >
                      <template #suffix>
                        <n-button text @click="browseApplication">
                          <n-icon :component="FolderOpenOutline" />
                        </n-button>
                      </template>
                    </n-input>
                  </n-form-item>

                  <n-form-item path="serviceName" label="系统服务">
                    <n-select
                      v-model:value="formData.serviceName"
                      :options="serviceOptions"
                      placeholder="选择系统服务 (可选)"
                      filterable
                      clearable
                    />
                  </n-form-item>
                </div>
              </div>
            </div>
          </n-tab-pane>

          <!-- 协议和端口 -->
          <n-tab-pane name="protocol" tab="协议端口">
            <div class="tab-content">
              <div class="form-section">
                <div class="section-title">
                  <n-icon :component="LayersOutline" />
                  协议设置
                </div>
                <div class="form-grid">
                  <n-form-item path="protocol" label="协议类型" required>
                    <n-select
                      v-model:value="formData.protocol"
                      :options="protocolOptions"
                      placeholder="选择协议类型"
                    />
                  </n-form-item>

                  <div class="form-row">
                    <n-form-item path="localPorts" label="本地端口">
                      <n-input
                        v-model:value="formData.localPorts"
                        placeholder="例: 80, 443, 8080-8090 (留空表示所有端口)"
                      />
                    </n-form-item>

                    <n-form-item path="remotePorts" label="远程端口">
                      <n-input
                        v-model:value="formData.remotePorts"
                        placeholder="例: 80, 443, 8080-8090 (留空表示所有端口)"
                      />
                    </n-form-item>
                  </div>

                  <n-form-item
                    v-if="formData.protocol === 1 || formData.protocol === 58"
                    path="icmpTypesAndCodes"
                    label="ICMP 类型和代码"
                  >
                    <n-input
                      v-model:value="formData.icmpTypesAndCodes"
                      placeholder="例: 8:0 (留空表示所有ICMP类型)"
                    />
                  </n-form-item>
                </div>
              </div>
            </div>
          </n-tab-pane>

          <!-- 地址设置 -->
          <n-tab-pane name="address" tab="地址设置">
            <div class="tab-content">
              <div class="form-section">
                <div class="section-title">
                  <n-icon :component="GlobeOutline" />
                  IP 地址范围
                </div>
                <div class="form-grid">
                  <n-form-item path="localAddresses" label="本地地址">
                    <n-input
                      v-model:value="formData.localAddresses"
                      placeholder="例: 192.168.1.0/24, 10.0.0.1 (* 表示任何地址)"
                    />
                    <template #feedback> 支持单个IP、CIDR格式或逗号分隔的多个地址</template>
                  </n-form-item>

                  <n-form-item path="remoteAddresses" label="远程地址">
                    <n-input
                      v-model:value="formData.remoteAddresses"
                      placeholder="例: 192.168.1.0/24, 10.0.0.1 (* 表示任何地址)"
                    />
                    <template #feedback> 支持单个IP、CIDR格式或逗号分隔的多个地址</template>
                  </n-form-item>
                </div>
              </div>
            </div>
          </n-tab-pane>

          <!-- 配置文件和接口 -->
          <n-tab-pane name="profile" tab="配置文件">
            <div class="tab-content">
              <div class="form-section">
                <div class="section-title">
                  <n-icon :component="ShieldCheckmarkOutline" />
                  网络配置文件
                </div>
                <div class="form-grid">
                  <n-form-item path="profiles" label="应用于配置文件" required>
                    <n-checkbox-group v-model:value="formData.profiles">
                      <n-space vertical>
                        <n-checkbox :value="1">
                          <div class="profile-option">
                            <div class="profile-name">域网络 (Domain)</div>
                            <div class="profile-desc">计算机连接到域时</div>
                          </div>
                        </n-checkbox>
                        <n-checkbox :value="2">
                          <div class="profile-option">
                            <div class="profile-name">专用网络 (Private)</div>
                            <div class="profile-desc">家庭或工作网络</div>
                          </div>
                        </n-checkbox>
                        <n-checkbox :value="4">
                          <div class="profile-option">
                            <div class="profile-name">公用网络 (Public)</div>
                            <div class="profile-desc">公共场所的网络</div>
                          </div>
                        </n-checkbox>
                      </n-space>
                    </n-checkbox-group>
                  </n-form-item>

                  <n-form-item path="interfaceTypes" label="接口类型">
                    <n-input
                      v-model:value="formData.interfaceTypes"
                      placeholder="例: Wireless, Lan (留空或'All'表示所有接口)"
                    />
                  </n-form-item>

                  <n-form-item path="interfaces" label="特定接口">
                    <n-dynamic-tags
                      v-model:value="formData.interfaces"
                      placeholder="添加网络接口名称"
                    />
                    <template #feedback> 可以指定特定的网络接口，留空表示所有接口</template>
                  </n-form-item>
                </div>
              </div>
            </div>
          </n-tab-pane>

          <!-- 高级选项 -->
          <n-tab-pane name="advanced" tab="高级选项">
            <div class="tab-content">
              <div class="form-section">
                <div class="section-title">
                  <n-icon :component="CogOutline" />
                  边缘遍历设置
                </div>
                <div class="form-grid">
                  <n-form-item path="edgeTraversal" label="边缘遍历">
                    <n-checkbox v-model:checked="formData.edgeTraversal">
                      允许边缘遍历 (适用于需要穿越NAT的应用)
                    </n-checkbox>
                  </n-form-item>

                  <n-form-item path="edgeTraversalAllowed" label="边缘遍历策略">
                    <n-checkbox v-model:checked="formData.edgeTraversalAllowed">
                      允许延迟边缘遍历
                    </n-checkbox>
                  </n-form-item>
                </div>
              </div>

              <div class="form-section">
                <div class="section-title">
                  <n-icon :component="LockClosedOutline" />
                  授权设置
                </div>
                <div class="form-grid">
                  <n-form-item path="remoteMachineAuthorizationList" label="远程计算机授权">
                    <n-input
                      v-model:value="formData.remoteMachineAuthorizationList"
                      placeholder="输入安全描述符 (高级用户)"
                      type="textarea"
                      :rows="2"
                    />
                  </n-form-item>

                  <n-form-item path="remoteUserAuthorizationList" label="远程用户授权">
                    <n-input
                      v-model:value="formData.remoteUserAuthorizationList"
                      placeholder="输入安全描述符 (高级用户)"
                      type="textarea"
                      :rows="2"
                    />
                  </n-form-item>

                  <n-form-item path="embeddedContext" label="嵌入式上下文">
                    <n-input
                      v-model:value="formData.embeddedContext"
                      placeholder="可选的嵌入式上下文信息"
                    />
                  </n-form-item>
                </div>
              </div>
            </div>
          </n-tab-pane>
        </n-tabs>
      </n-form>
    </div>

    <template #footer>
      <div class="modal-footer">
        <n-space>
          <n-button @click="handleClose">取消</n-button>
          <n-button @click="handleValidate">验证规则</n-button>
          <n-button type="primary" @click="handleSave" :loading="saving">
            <template #icon>
              <n-icon :component="CheckmarkOutline" />
            </template>
            {{ isEdit ? '更新规则' : '创建规则' }}
          </n-button>
        </n-space>
      </div>
    </template>
  </n-modal>
</template>

<script setup lang="ts">
import { ref, computed, watch, nextTick } from 'vue'
import {
  NModal,
  NTabs,
  NTabPane,
  NForm,
  NFormItem,
  NInput,
  NSelect,
  NRadioGroup,
  NRadio,
  NCheckbox,
  NCheckboxGroup,
  NDynamicTags,
  NButton,
  NIcon,
  NSpace,
  FormInst,
  FormRules,
  useMessage,
} from 'naive-ui'
import {
  CheckmarkOutline,
  InformationCircleOutline,
  SettingsOutline,
  AppsOutline,
  LayersOutline,
  GlobeOutline,
  ShieldCheckmarkOutline,
  CogOutline,
  LockClosedOutline,
  FolderOpenOutline,
} from '@vicons/ionicons5'

// 枚举定义 - 对应后端模型
enum RuleDirection {
  Inbound = 1,
  Outbound = 2,
}

enum RuleAction {
  Block = 0,
  Allow = 1,
}

enum ProtocolType {
  Any = 256,
  ICMPV4 = 1,
  IGMP = 2,
  TCP = 6,
  UDP = 17,
  IPv6 = 41,
  IPv6Route = 43,
  IPv6Frag = 44,
  GRE = 47,
  ICMPv6 = 58,
  IPv6NoNxt = 59,
  IPv6Opts = 60,
  VRRP = 112,
  PGM = 113,
  L2TP = 115,
}

enum FirewallProfile {
  Domain = 1,
  Private = 2,
  Public = 4,
}

// 防火墙规则接口 - 对应后端 FirewallRule
interface FirewallRule {
  name: string
  description: string
  applicationName: string
  serviceName: string
  protocol: ProtocolType
  localPorts: string
  remotePorts: string
  localAddresses: string
  remoteAddresses: string
  icmpTypesAndCodes: string
  direction: RuleDirection
  enabled: boolean
  profiles: FirewallProfile[]
  edgeTraversal: boolean
  action: RuleAction
  grouping: string
  interfaceTypes: string
  interfaces: string[]

  // 高级属性
  edgeTraversalAllowed: boolean
  looseSourceMapping: boolean
  localOnlyMapping: boolean
  remoteMachineAuthorizationList: string
  remoteUserAuthorizationList: string
  embeddedContext: string
  flags: number
  secureFlags: boolean
}

// 创建规则请求接口 - 对应后端 CreateRuleRequest
interface CreateRuleRequest extends Omit<FirewallRule, 'profiles'> {
  profiles: number // 使用位标志表示多个配置文件
}

const props = defineProps<{
  modelValue: boolean
  editRule?: FirewallRule | null
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  save: [rule: CreateRuleRequest]
}>()

// 响应式数据
const message = useMessage()
const formRef = ref<FormInst>()
const visible = computed({
  get: () => props.modelValue,
  set: (value) => emit('update:modelValue', value),
})

const isEdit = computed(() => !!props.editRule)
const activeTab = ref('basic')
const saving = ref(false)

// 表单数据
const defaultFormData: FirewallRule = {
  name: '',
  description: '',
  applicationName: '',
  serviceName: '',
  protocol: ProtocolType.Any,
  localPorts: '',
  remotePorts: '',
  localAddresses: '*',
  remoteAddresses: '*',
  icmpTypesAndCodes: '',
  direction: RuleDirection.Inbound,
  enabled: true,
  profiles: [FirewallProfile.Domain, FirewallProfile.Private],
  edgeTraversal: false,
  action: RuleAction.Allow,
  grouping: '',
  interfaceTypes: 'All',
  interfaces: [],

  // 高级属性
  edgeTraversalAllowed: false,
  looseSourceMapping: false,
  localOnlyMapping: false,
  remoteMachineAuthorizationList: '',
  remoteUserAuthorizationList: '',
  embeddedContext: '',
  flags: 0,
  secureFlags: false,
}

const formData = ref<FirewallRule>({ ...defaultFormData })

// 表单验证规则
const formRules: FormRules = {
  name: [
    { required: true, message: '请输入规则名称', trigger: 'blur' },
    { min: 1, max: 255, message: '规则名称长度应在1-255字符之间', trigger: 'blur' },
  ],
  direction: [{ required: true, message: '请选择规则方向', trigger: 'change', type: 'number' }],
  action: [{ required: true, message: '请选择规则动作', trigger: 'change', type: 'number' }],
  protocol: [{ required: true, message: '请选择协议类型', trigger: 'change', type: 'number' }],
  profiles: [
    {
      required: true,
      message: '请至少选择一个配置文件',
      trigger: 'change',
      validator: (rule, value: FirewallProfile[]) => {
        return value && value.length > 0
      },
    },
  ],
}

// 选项数据
const protocolOptions = [
  { label: '任意协议', value: ProtocolType.Any },
  { label: 'TCP', value: ProtocolType.TCP },
  { label: 'UDP', value: ProtocolType.UDP },
  { label: 'ICMPv4', value: ProtocolType.ICMPV4 },
  { label: 'ICMPv6', value: ProtocolType.ICMPv6 },
  { label: 'IGMP', value: ProtocolType.IGMP },
  { label: 'IPv6', value: ProtocolType.IPv6 },
  { label: 'GRE', value: ProtocolType.GRE },
  { label: 'VRRP', value: ProtocolType.VRRP },
  { label: 'PGM', value: ProtocolType.PGM },
  { label: 'L2TP', value: ProtocolType.L2TP },
]

const serviceOptions = [
  { label: 'Windows Update (wuauserv)', value: 'wuauserv' },
  { label: '远程桌面服务 (TermService)', value: 'TermService' },
  { label: 'Windows Defender (WinDefend)', value: 'WinDefend' },
  { label: '打印后台处理程序 (Spooler)', value: 'Spooler' },
  { label: 'Windows 防火墙 (MpsSvc)', value: 'MpsSvc' },
  { label: 'DNS 客户端 (Dnscache)', value: 'Dnscache' },
  { label: 'DHCP 客户端 (Dhcp)', value: 'Dhcp' },
  { label: 'Workstation (LanmanWorkstation)', value: 'LanmanWorkstation' },
  { label: 'Server (LanmanServer)', value: 'LanmanServer' },
]

// 监听编辑规则变化
watch(
  () => props.editRule,
  (newRule) => {
    if (newRule) {
      formData.value = { ...newRule }
    } else {
      formData.value = { ...defaultFormData }
    }
  },
  { immediate: true },
)

// 方法
const handleClose = () => {
  emit('update:modelValue', false)
  setTimeout(() => {
    formData.value = { ...defaultFormData }
    activeTab.value = 'basic'
    formRef.value?.restoreValidation()
  }, 300)
}

const handleValidate = async () => {
  try {
    await formRef.value?.validate()
    message.success('规则验证通过')
  } catch (errors) {
    message.error('请检查表单中的错误')
    console.log(errors)
  }
}

const handleSave = async () => {
  try {
    await formRef.value?.validate()
    saving.value = true

    // 转换数据格式以匹配后端要求
    const saveData: CreateRuleRequest = {
      ...formData.value,
      // 将配置文件数组转换为位标志
      profiles: formData.value.profiles.reduce((acc, profile) => acc | profile, 0),
    }

    // 模拟保存延迟
    await new Promise((resolve) => setTimeout(resolve, 1000))

    emit('save', saveData)
    message.success(isEdit.value ? '规则更新成功' : '规则创建成功')
    handleClose()
  } catch (errors) {
    message.error('请检查表单中的错误')
    console.log(errors)
  } finally {
    saving.value = false
  }
}

const browseApplication = () => {
  // 这里可以集成文件选择器或调用 Electron API
  message.info('请使用文件浏览器选择应用程序')
}
</script>

<style scoped>
/* 基础模态框样式 */
:deep(.firewall-rule-modal .n-card) {
  background: var(--bg-glass);
  backdrop-filter: var(--backdrop-blur);
  border: 1px solid var(--border-primary);
  box-shadow: var(--shadow-xl);
  display: flex;
  flex-direction: column;
}

:deep(.firewall-rule-modal .n-card__header) {
  background: var(--bg-card);
  border-bottom: 1px solid var(--border-secondary);
  padding: 20px 24px;
  flex-shrink: 0;
}

:deep(.firewall-rule-modal .n-card__content) {
  padding: 0;
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
}

:deep(.firewall-rule-modal .n-card__content),
.rule-form-container,
.firewall-form,
.rule-tabs,
:deep(.rule-tabs .n-tabs-pane-wrapper),
:deep(.rule-tabs .n-tabs-pane) {
  overflow: hidden;
}

:deep(.firewall-rule-modal .n-card__footer) {
  background: var(--bg-card);
  border-top: 1px solid var(--border-secondary);
  padding: 16px 24px;
  flex-shrink: 0;
}

/* 表单容器 */
.rule-form-container {
  height: 100%;
  display: flex;
  flex-direction: column;
  min-height: 0;
}

.firewall-form {
  height: 100%;
  display: flex;
  flex-direction: column;
  min-height: 0;
}

/* 标签页样式 */
.rule-tabs {
  height: 100%;
  display: flex;
  flex-direction: column;
  min-height: 0;
}

:deep(.rule-tabs .n-tabs-nav) {
  background: var(--bg-secondary);
  border-bottom: 1px solid var(--border-secondary);
  padding: 0 24px;
  flex-shrink: 0;
  height: 60px;
  display: flex;
  align-items: center;
}

:deep(.rule-tabs .n-tabs-pane-wrapper) {
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
}

:deep(.rule-tabs .n-tabs-pane) {
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
}

/* 标签页内容 */
.tab-content {
  flex: 1;
  overflow-y: auto;
  padding: 24px;
  scroll-behavior: smooth;
  min-height: 0;
  /* 新增：计算可用高度 */
  max-height: calc(90vh - 220px);
}

.tab-content::-webkit-scrollbar {
  width: 6px;
}

.tab-content::-webkit-scrollbar-track {
  background: transparent;
}

.tab-content::-webkit-scrollbar-thumb {
  background: var(--border-secondary);
  border-radius: 3px;
  opacity: 0.6;
}

.tab-content::-webkit-scrollbar-thumb:hover {
  background: var(--border-hover);
  opacity: 1;
}

/* 表单区块样式 */
.form-section {
  margin-bottom: 28px;
  background: var(--bg-card);
  border: 1px solid var(--border-secondary);
  border-radius: 12px;
  padding: 20px;
  transition: all 0.3s ease;
}

.form-section:hover {
  border-color: var(--border-hover);
  box-shadow: var(--shadow-sm);
}

.form-section:last-child {
  margin-bottom: 0;
}

/* 区块标题 */
.section-title {
  font-size: 14px;
  font-weight: 600;
  color: var(--text-quaternary);
  margin: 0 0 16px 0;
  padding-bottom: 8px;
  border-bottom: 1px solid var(--border-tertiary);
  display: flex;
  align-items: center;
  gap: 8px;
}

.section-title .n-icon {
  color: var(--accent-primary);
  background: rgba(59, 130, 246, 0.1);
  padding: 4px;
  border-radius: 4px;
  font-size: 14px;
}

/* 表单网格 */
.form-grid {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.form-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 20px;
}

/* 配置文件选项样式 */
.profile-option {
  margin-left: 8px;
}

.profile-name {
  font-size: 13px;
  font-weight: 500;
  color: var(--text-secondary);
  margin-bottom: 2px;
}

.profile-desc {
  font-size: 11px;
  color: var(--text-muted);
  line-height: 1.4;
}

/* 模态框底部 */
.modal-footer {
  display: flex;
  justify-content: flex-end;
}

/* 标签页切换动画 */
:deep(.n-tabs-tab) {
  transition: all 0.3s ease;
  border-radius: 6px;
  padding: 12px 16px;
  font-weight: 500;
  color: var(--text-muted);
}

:deep(.n-tabs-tab:hover) {
  color: var(--text-secondary);
  background: var(--bg-hover);
}

:deep(.n-tabs-tab--active) {
  color: var(--accent-primary) !important;
  background: var(--bg-card);
  font-weight: 600;
}

/* Naive UI 组件样式覆盖 */
:deep(.n-form-item-label) {
  font-weight: 500;
  color: var(--text-muted);
  font-size: 12px;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  margin-bottom: 8px;
}

:deep(.n-form-item-feedback) {
  font-size: 11px;
  color: var(--text-muted);
  font-style: italic;
  margin-top: 4px;
}

:deep(.n-input) {
  --n-border: 1px solid var(--border-tertiary);
  --n-border-hover: 1px solid var(--border-hover);
  --n-border-focus: 1px solid var(--accent-primary);
  --n-color: var(--bg-card);
  --n-text-color: var(--text-secondary);
  --n-border-radius: 6px;
  --n-font-size: 13px;
}

:deep(.n-select) {
  --n-border: 1px solid var(--border-tertiary);
  --n-border-hover: 1px solid var(--border-hover);
  --n-border-focus: 1px solid var(--accent-primary);
  --n-color: var(--bg-card);
  --n-text-color: var(--text-secondary);
  --n-border-radius: 6px;
  --n-font-size: 13px;
}

:deep(.n-checkbox) {
  --n-border: 1px solid var(--border-tertiary);
  --n-border-checked: 1px solid var(--accent-primary);
  --n-color-checked: var(--accent-primary);
  --n-font-size: 13px;
}

:deep(.n-radio) {
  --n-dot-color-active: var(--accent-primary);
  --n-color-active: var(--accent-primary);
  --n-font-size: 13px;
}

:deep(.n-checkbox-group) {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

:deep(.n-radio-group) {
  display: flex;
  gap: 20px;
  flex-wrap: wrap;
}

/* 标签页内容切换动画 */
.tab-content {
  animation: fadeInSlide 0.3s ease-out;
}

@keyframes fadeInSlide {
  from {
    opacity: 0;
    transform: translateY(10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

/* 响应式设计 */
@media (max-width: 768px) {
  :deep(.firewall-rule-modal .n-card) {
    width: 90vw !important;
    height: 90vh !important;
  }

  .tab-content {
    padding: 16px;
  }

  .form-row {
    grid-template-columns: 1fr;
    gap: 16px;
  }

  .form-section {
    padding: 16px;
    margin-bottom: 20px;
  }

  :deep(.n-radio-group) {
    flex-direction: column;
    gap: 12px;
  }

  :deep(.rule-tabs .n-tabs-nav) {
    padding: 0 16px;
  }
}

/* 深色模式适配 */
@media (prefers-color-scheme: dark) {
  .tab-content::-webkit-scrollbar-thumb {
    background: rgba(255, 255, 255, 0.1);
  }

  .tab-content::-webkit-scrollbar-thumb:hover {
    background: rgba(255, 255, 255, 0.2);
  }
}
</style>
