<template>
  <div class="monitor-view">
    <!-- 主内容区域 - 可拖拽布局 -->
    <div class="main-content">
      <!-- 左侧边栏 -->
      <div class="sidebar scrollbar-primary scrollbar-thin" :style="{ width: sidebarWidth + 'px' }">
        <div class="sidebar-content">
          <div class="sidebar-header">
            <h3 class="sidebar-title">系统概览</h3>
            <div class="sidebar-badge">{{ mockData.processCount }}</div>
          </div>

          <div class="sidebar-stats">
            <div class="stat-card">
              <div class="stat-icon">🔗</div>
              <div class="stat-info">
                <div class="stat-value">{{ mockData.activeConnections }}</div>
                <div class="stat-label">活跃连接</div>
              </div>
            </div>

            <div class="stat-card">
              <div class="stat-icon">⚡</div>
              <div class="stat-info">
                <div class="stat-value">{{ mockData.networkSpeed }}</div>
                <div class="stat-label">网络速度</div>
              </div>
            </div>

            <div class="stat-card">
              <div class="stat-icon">🛡️</div>
              <div class="stat-info">
                <div class="stat-value">{{ mockData.ruleCount }}</div>
                <div class="stat-label">防护规则</div>
              </div>
            </div>
          </div>

          <div class="sidebar-section">
            <h4 class="section-title">快速操作</h4>
            <div class="quick-actions">
              <div class="action-item">扫描威胁</div>
              <div class="action-item">更新规则</div>
              <div class="action-item">导出日志</div>
            </div>
          </div>
        </div>
      </div>

      <!-- 左侧分割线 -->
      <div class="resize-handle-vertical" @mousedown="startResize('sidebar', $event)">
        <div class="resize-handle-hover"></div>
      </div>

      <!-- 中间主视图区域 -->
      <div class="main-view">
        <!-- 中间顶部 - 搜索和标题 -->
        <div class="main-header" :style="{ height: mainHeaderHeight + 'px' }">
          <div class="header-content">
            <div class="header-info">
              <h2 class="main-header-title">网络连接监控</h2>
              <p class="main-header-subtitle">实时监控系统网络活动</p>
            </div>

            <div class="header-actions">
              <div class="search-area">
                <n-input
                  v-model:value="searchQuery"
                  placeholder="搜索连接、域名或IP..."
                  size="medium"
                  round
                >
                  <template #prefix>
                    <n-icon :component="SearchOutline" />
                  </template>
                </n-input>
              </div>

              <div class="filter-buttons">
                <n-button size="small" type="primary" ghost>过滤</n-button>
                <n-button size="small" ghost>刷新</n-button>
              </div>
            </div>
          </div>
        </div>

        <!-- 中间主体 - 连接列表表格 -->
        <div class="connections-area">
          <div class="connections-header">
            <h3 class="connections-title">
              <span class="title-icon">📊</span>
              连接列表
              <span class="connection-count">{{ mockData.activeConnections }} 个活跃连接</span>
            </h3>
            <div class="connections-controls">
              <n-button size="small" type="info" ghost>暂停监控</n-button>
            </div>
          </div>

          <div class="connections-content">
            <div class="table-container">
              <div class="table-header">
                <div class="table-column">进程</div>
                <div class="table-column">本地地址</div>
                <div class="table-column">远程地址</div>
                <div class="table-column">状态</div>
                <div class="table-column">流量</div>
              </div>

              <div class="table-body scrollbar-success scrollbar-glow">
                <div
                  v-for="i in 15"
                  :key="i"
                  class="table-row"
                  :class="{ 'row-selected': i === 3 }"
                >
                  <div class="table-cell">
                    <div class="process-info">
                      <div class="process-icon">🌐</div>
                      <span>chrome.exe</span>
                    </div>
                  </div>
                  <div class="table-cell">192.168.1.{{ 100 + i }}</div>
                  <div class="table-cell">
                    {{ ['google.com', 'github.com', 'cloudflare.com'][i % 3] }}
                  </div>
                  <div class="table-cell">
                    <span class="status-badge established">已建立</span>
                  </div>
                  <div class="table-cell">
                    <span class="traffic-info">{{ (Math.random() * 1000).toFixed(0) }} KB/s</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- 时间轴分割线 -->
        <div class="resize-handle-horizontal" @mousedown="startResize('timeline', $event)">
          <div class="resize-handle-hover-h"></div>
        </div>

        <!-- 中间底部 - 时间轴事件流 -->
        <div class="timeline-area" :style="{ height: timelineHeight + 'px' }">
          <div class="timeline-header">
            <h3 class="timeline-title">
              <span class="title-icon">📈</span>
              实时事件流
            </h3>
            <div class="timeline-controls">
              <n-button size="small" type="warning" ghost>暂停</n-button>
              <n-button size="small" ghost>清空</n-button>
            </div>
          </div>

          <div class="timeline-content scrollbar-warning scrollbar-animated">
            <div class="events-container">
              <div
                v-for="i in 12"
                :key="i"
                class="event-item"
                :class="['event-' + ['info', 'warning', 'success'][i % 3]]"
              >
                <div class="event-time">{{ new Date().toLocaleTimeString() }}</div>
                <div class="event-content">
                  <div class="event-type">{{ ['连接建立', '数据传输', '连接关闭'][i % 3] }}</div>
                  <div class="event-desc">chrome.exe → google.com:443</div>
                </div>
                <div class="event-indicator"></div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- 右侧分割线 -->
      <div class="resize-handle-vertical" @mousedown="startResize('inspector', $event)">
        <div class="resize-handle-hover"></div>
      </div>

      <!-- 右侧检查器面板 -->
      <div class="inspector scrollbar-purple scrollbar-glow" :style="{ width: inspectorWidth + 'px' }">
        <div class="inspector-content">
          <div class="inspector-header">
            <h3 class="inspector-title">
              <span class="title-icon">🔍</span>
              连接详情
            </h3>
          </div>

          <div class="inspector-body">
            <div class="detail-section">
              <h4 class="detail-section-title">基本信息</h4>
              <div class="detail-grid">
                <div class="detail-item">
                  <span class="detail-label">进程</span>
                  <span class="detail-value">chrome.exe</span>
                </div>
                <div class="detail-item">
                  <span class="detail-label">PID</span>
                  <span class="detail-value">12345</span>
                </div>
                <div class="detail-item">
                  <span class="detail-label">协议</span>
                  <span class="detail-value">TCP</span>
                </div>
                <div class="detail-item">
                  <span class="detail-label">状态</span>
                  <span class="detail-value status-established">已建立</span>
                </div>
              </div>
            </div>

            <div class="detail-section">
              <h4 class="detail-section-title">网络信息</h4>
              <div class="network-info">
                <div class="network-item">
                  <div class="network-label">本地地址</div>
                  <div class="network-value">192.168.1.100:54321</div>
                </div>
                <div class="network-item">
                  <div class="network-label">远程地址</div>
                  <div class="network-value">142.250.191.14:443</div>
                </div>
                <div class="network-item">
                  <div class="network-label">域名</div>
                  <div class="network-value">google.com</div>
                </div>
              </div>
            </div>

            <div class="detail-section">
              <h4 class="detail-section-title">流量统计</h4>
              <div class="traffic-stats">
                <div class="traffic-item">
                  <span class="traffic-label">上传</span>
                  <span class="traffic-value upload">1.2 MB</span>
                </div>
                <div class="traffic-item">
                  <span class="traffic-label">下载</span>
                  <span class="traffic-value download">5.8 MB</span>
                </div>
                <div class="traffic-item">
                  <span class="traffic-label">总计</span>
                  <span class="traffic-value total">7.0 MB</span>
                </div>
              </div>

              <div class="traffic-chart">
                <div class="chart-bars">
                  <div
                    v-for="i in 20"
                    :key="i"
                    class="chart-bar"
                    :style="{ height: Math.random() * 100 + '%' }"
                  ></div>
                </div>
              </div>
            </div>

            <div class="detail-section">
              <h4 class="detail-section-title">安全信息</h4>
              <div class="security-info">
                <div class="security-item safe">
                  <div class="security-icon">✅</div>
                  <div class="security-text">
                    <div class="security-title">连接安全</div>
                    <div class="security-desc">HTTPS 加密连接</div>
                  </div>
                </div>
                <div class="security-item safe">
                  <div class="security-icon">🛡️</div>
                  <div class="security-text">
                    <div class="security-title">证书有效</div>
                    <div class="security-desc">由 Google Trust Services 签发</div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import { NButton, NInput, NIcon } from 'naive-ui'
import { SearchOutline } from '@vicons/ionicons5'

// 响应式数据
const searchQuery = ref('')

// 布局尺寸控制
const sidebarWidth = ref(300)
const inspectorWidth = ref(350)
const timelineHeight = ref(500)
const mainHeaderHeight = ref(80)

// 调整分割条范围限制 - 放宽范围
const MIN_SIDEBAR_WIDTH = 200
const MAX_SIDEBAR_WIDTH = 600
const MIN_INSPECTOR_WIDTH = 250
const MAX_INSPECTOR_WIDTH = 650
const MIN_TIMELINE_HEIGHT = 100
const MAX_TIMELINE_HEIGHT = 800

// 模拟数据
const mockData = ref({
  activeConnections: 247,
  processCount: 43,
  networkSpeed: '5.2 MB/s',
  ruleCount: 156,
  selectedConnection: 'chrome.exe → google.com',
  connectionStatus: '已建立',
  localIP: '192.168.1.100',
})

// 拖拽状态
const resizing = ref<{
  type: 'sidebar' | 'inspector' | 'timeline' | null
  startX: number
  startY: number
  startWidth: number
  startHeight: number
}>({
  type: null,
  startX: 0,
  startY: 0,
  startWidth: 0,
  startHeight: 0,
})

// 开始拖拽
const startResize = (type: 'sidebar' | 'inspector' | 'timeline', event: MouseEvent) => {
  event.preventDefault()

  resizing.value = {
    type,
    startX: event.clientX,
    startY: event.clientY,
    startWidth:
      type === 'sidebar' ? sidebarWidth.value : type === 'inspector' ? inspectorWidth.value : 0,
    startHeight: type === 'timeline' ? timelineHeight.value : 0,
  }

  document.addEventListener('mousemove', handleResize)
  document.addEventListener('mouseup', stopResize)
  document.body.style.cursor = type === 'timeline' ? 'row-resize' : 'col-resize'
  document.body.style.userSelect = 'none'
}

// 处理拖拽
const handleResize = (event: MouseEvent) => {
  if (!resizing.value.type) return

  const { type, startX, startY, startWidth, startHeight } = resizing.value

  if (type === 'sidebar') {
    const deltaX = event.clientX - startX
    const newWidth = Math.min(MAX_SIDEBAR_WIDTH, Math.max(MIN_SIDEBAR_WIDTH, startWidth + deltaX))
    sidebarWidth.value = newWidth
  } else if (type === 'inspector') {
    const deltaX = startX - event.clientX
    const newWidth = Math.min(
      MAX_INSPECTOR_WIDTH,
      Math.max(MIN_INSPECTOR_WIDTH, startWidth + deltaX),
    )
    inspectorWidth.value = newWidth
  } else if (type === 'timeline') {
    const deltaY = startY - event.clientY
    const newHeight = Math.min(
      MAX_TIMELINE_HEIGHT,
      Math.max(MIN_TIMELINE_HEIGHT, startHeight + deltaY),
    )
    timelineHeight.value = newHeight
  }
}

// 停止拖拽
const stopResize = () => {
  resizing.value.type = null
  document.removeEventListener('mousemove', handleResize)
  document.removeEventListener('mouseup', stopResize)
  document.body.style.cursor = ''
  document.body.style.userSelect = ''
}

// 重置布局
const resetLayout = () => {
  sidebarWidth.value = 300
  inspectorWidth.value = 450
  timelineHeight.value = 200
}

// 键盘快捷键
onMounted(() => {
  const handleKeydown = (event: KeyboardEvent) => {
    if (event.ctrlKey && event.key === 'r') {
      event.preventDefault()
      resetLayout()
    }
  }

  document.addEventListener('keydown', handleKeydown)

  onUnmounted(() => {
    document.removeEventListener('keydown', handleKeydown)
    document.removeEventListener('mousemove', handleResize)
    document.removeEventListener('mouseup', stopResize)
  })
})
</script>

<style scoped>
/* 监控视图容器 - 防止整体滚动 */
.monitor-view {
  height: 100vh;
  width: 100vw;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  position: fixed;
  top: 0;
  left: 0;
}

/* 主内容区域 */
.main-content {
  flex: 1;
  display: flex;
  min-height: 0;
  overflow: hidden;
}

/* 侧边栏 */
.sidebar {
  background: var(--bg-glass);
  backdrop-filter: var(--backdrop-blur);
  border-right: 1px solid var(--border-primary);
  overflow: hidden;
  display: flex;
  flex-direction: column;
  flex-shrink: 0;
}

.sidebar-content {
  padding: 24px;
  overflow-y: auto;
  flex: 1;
  min-height: 0;
}

.sidebar-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;
}

.sidebar-title {
  font-size: 16px;
  font-weight: 600;
  color: var(--text-secondary);
  margin: 0;
}

.sidebar-badge {
  background: linear-gradient(135deg, var(--accent-primary) 0%, #1d4ed8 100%);
  color: white;
  padding: 4px 8px;
  border-radius: 12px;
  font-size: 12px;
  font-weight: 600;
}

.sidebar-stats {
  display: flex;
  flex-direction: column;
  gap: 12px;
  margin-bottom: 24px;
}

.stat-card {
  background: var(--bg-card);
  border: 1px solid var(--border-tertiary);
  border-radius: 12px;
  padding: 16px;
  display: flex;
  align-items: center;
  gap: 12px;
  transition: var(--transition);
}

.stat-card:hover {
  background: var(--bg-hover);
  border-color: var(--border-hover);
  transform: translateY(-1px);
}

.stat-icon {
  font-size: 20px;
  width: 32px;
  text-align: center;
}

.stat-info {
  flex: 1;
}

.stat-value {
  font-size: 18px;
  font-weight: 700;
  color: var(--text-secondary);
  line-height: 1;
}

.stat-label {
  font-size: 12px;
  color: var(--text-muted);
  margin-top: 2px;
}

.sidebar-section {
  margin-top: 24px;
}

.section-title {
  font-size: 14px;
  font-weight: 600;
  color: var(--text-quaternary);
  margin-bottom: 12px;
}

.quick-actions {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.action-item {
  padding: 8px 12px;
  background: var(--bg-tertiary);
  border-radius: 8px;
  font-size: 13px;
  color: var(--text-quaternary);
  cursor: pointer;
  transition: var(--transition);
}

.action-item:hover {
  background: var(--bg-hover);
  color: var(--text-secondary);
}

/* 拖拽分割线 */
.resize-handle-vertical {
  width: 4px;
  background: var(--border-tertiary);
  cursor: col-resize;
  transition: background-color 0.2s;
  position: relative;
  z-index: 10;
  flex-shrink: 0;
}

.resize-handle-vertical:hover {
  background: var(--border-hover);
}

.resize-handle-hover {
  position: absolute;
  inset: 0 -4px;
}

.resize-handle-horizontal {
  height: 4px;
  background: var(--border-tertiary);
  cursor: row-resize;
  transition: background-color 0.2s;
  position: relative;
  z-index: 10;
  flex-shrink: 0;
}

.resize-handle-horizontal:hover {
  background: rgba(168, 85, 247, 0.5);
}

.resize-handle-hover-h {
  position: absolute;
  inset: -4px 0;
}

/* 主视图 */
.main-view {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 0;
  min-height: 0;
  background: var(--bg-card);
  overflow: hidden;
}

.main-header {
  background: var(--bg-glass);
  backdrop-filter: var(--backdrop-blur);
  border-bottom: 1px solid var(--border-primary);
  padding: 0 24px;
  flex-shrink: 0;
}

.header-content {
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.header-info {
  flex: 1;
}

.main-header-title {
  font-size: 20px;
  font-weight: 700;
  color: var(--text-secondary);
  margin: 0;
  line-height: 1;
}

.main-header-subtitle {
  font-size: 14px;
  color: var(--text-muted);
  margin: 4px 0 0 0;
}

.header-actions {
  display: flex;
  align-items: center;
  gap: 16px;
}

.search-area {
  width: 300px;
}

.filter-buttons {
  display: flex;
  gap: 8px;
}

/* 连接区域 */
.connections-area {
  flex: 1;
  background: var(--bg-glass);
  overflow: hidden;
  display: flex;
  flex-direction: column;
  min-height: 0;
}

.connections-header {
  padding: 16px 24px;
  border-bottom: 1px solid var(--border-secondary);
  display: flex;
  align-items: center;
  justify-content: space-between;
  background: var(--bg-card);
  flex-shrink: 0;
}

.connections-title {
  font-size: 16px;
  font-weight: 600;
  color: var(--text-secondary);
  margin: 0;
  display: flex;
  align-items: center;
  gap: 8px;
}

.title-icon {
  font-size: 16px;
}

.connection-count {
  font-size: 12px;
  color: var(--text-muted);
  font-weight: 400;
  margin-left: 8px;
}

.connections-content {
  flex: 1;
  overflow: hidden;
  padding: 0;
  min-height: 0;
}

.table-container {
  height: 100%;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.table-header {
  display: flex;
  padding: 12px 24px;
  background: var(--bg-card);
  border-bottom: 1px solid var(--border-tertiary);
  font-size: 12px;
  font-weight: 600;
  color: var(--text-quaternary);
  text-transform: uppercase;
  letter-spacing: 0.5px;
  flex-shrink: 0;
}

.table-column {
  flex: 1;
  padding: 0 8px;
}

.table-column:first-child {
  flex: 1.5;
}

.table-body {
  flex: 1;
  overflow-y: auto;
  min-height: 0;
}

.table-row {
  display: flex;
  padding: 12px 24px;
  border-bottom: 1px solid var(--border-secondary);
  transition: var(--transition);
  cursor: pointer;
}

.table-row:hover {
  background: var(--bg-hover);
}

.table-row.row-selected {
  background: var(--bg-selected);
  border-color: var(--border-hover);
}

.table-cell {
  flex: 1;
  padding: 0 8px;
  display: flex;
  align-items: center;
  font-size: 13px;
  color: var(--text-tertiary);
}

.table-cell:first-child {
  flex: 1.5;
}

.process-info {
  display: flex;
  align-items: center;
  gap: 8px;
}

.process-icon {
  font-size: 14px;
}

.status-badge {
  padding: 2px 8px;
  border-radius: 12px;
  font-size: 11px;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.status-badge.established {
  background: rgba(34, 197, 94, 0.2);
  color: var(--accent-success);
  border: 1px solid rgba(34, 197, 94, 0.3);
}

.traffic-info {
  color: var(--accent-secondary);
  font-weight: 500;
}

/* 时间轴区域 */
.timeline-area {
  background: var(--bg-glass);
  backdrop-filter: var(--backdrop-blur);
  border-top: 1px solid var(--border-primary);
  overflow: hidden;
  display: flex;
  flex-direction: column;
  flex-shrink: 0;
}

.timeline-header {
  padding: 16px 24px;
  border-bottom: 1px solid var(--border-secondary);
  display: flex;
  align-items: center;
  justify-content: space-between;
  background: var(--bg-card);
  flex-shrink: 0;
}

.timeline-title {
  font-size: 16px;
  font-weight: 600;
  color: var(--text-secondary);
  margin: 0;
  display: flex;
  align-items: center;
  gap: 8px;
}

.timeline-controls {
  display: flex;
  gap: 8px;
}

.timeline-content {
  flex: 1;
  overflow-y: auto;
  padding: 12px 24px;
  min-height: 0;
}

.events-container {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.event-item {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px 16px;
  background: var(--bg-card);
  border-radius: 8px;
  border-left: 3px solid transparent;
  transition: var(--transition);
  position: relative;
}

.event-item:hover {
  background: var(--bg-hover);
}

.event-item.event-info {
  border-left-color: var(--accent-primary);
}

.event-item.event-warning {
  border-left-color: var(--accent-warning);
}

.event-item.event-success {
  border-left-color: var(--accent-success);
}

.event-time {
  font-size: 11px;
  color: var(--text-muted);
  min-width: 70px;
  font-family: 'JetBrains Mono', 'Fira Code', monospace;
}

.event-content {
  flex: 1;
}

.event-type {
  font-size: 13px;
  font-weight: 500;
  color: var(--text-secondary);
  line-height: 1;
}

.event-desc {
  font-size: 12px;
  color: var(--text-muted);
  margin-top: 2px;
}

.event-indicator {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: var(--text-disabled);
}

.event-item.event-info .event-indicator {
  background: var(--accent-primary);
}

.event-item.event-warning .event-indicator {
  background: var(--accent-warning);
}

.event-item.event-success .event-indicator {
  background: var(--accent-success);
}

/* 检查器面板 */
.inspector {
  background: var(--bg-glass);
  backdrop-filter: var(--backdrop-blur);
  border-left: 1px solid var(--border-primary);
  overflow: hidden;
  flex-shrink: 0;
}

.inspector-content {
  height: 100%;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
}

.inspector-header {
  padding: 16px 24px;
  border-bottom: 1px solid var(--border-secondary);
  background: var(--bg-card);
  flex-shrink: 0;
}

.inspector-title {
  font-size: 16px;
  font-weight: 600;
  color: var(--text-secondary);
  margin: 0;
  display: flex;
  align-items: center;
  gap: 8px;
}

.inspector-body {
  flex: 1;
  padding: 24px;
  min-height: 0;
  overflow-y: auto;
}

.detail-section {
  margin-bottom: 24px;
}

.detail-section:last-child {
  margin-bottom: 0;
}

.detail-section-title {
  font-size: 14px;
  font-weight: 600;
  color: var(--text-quaternary);
  margin: 0 0 16px 0;
  padding-bottom: 8px;
  border-bottom: 1px solid var(--border-tertiary);
}

.detail-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 12px 16px;
}

.detail-item {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.detail-label {
  font-size: 11px;
  color: var(--text-muted);
  text-transform: uppercase;
  letter-spacing: 0.5px;
  font-weight: 600;
}

.detail-value {
  font-size: 13px;
  color: var(--text-secondary);
  font-weight: 500;
}

.detail-value.status-established {
  color: var(--accent-success);
}

.network-info {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.network-item {
  padding: 12px;
  background: var(--bg-card);
  border-radius: 8px;
  border: 1px solid var(--border-tertiary);
}

.network-label {
  font-size: 11px;
  color: var(--text-muted);
  text-transform: uppercase;
  letter-spacing: 0.5px;
  font-weight: 600;
  margin-bottom: 4px;
}

.network-value {
  font-size: 13px;
  color: var(--text-secondary);
  font-family: 'JetBrains Mono', 'Fira Code', monospace;
  font-weight: 500;
}

.traffic-stats {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin-bottom: 16px;
}

.traffic-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 8px 12px;
  background: var(--bg-card);
  border-radius: 6px;
}

.traffic-label {
  font-size: 12px;
  color: var(--text-muted);
  font-weight: 500;
}

.traffic-value {
  font-size: 13px;
  font-weight: 600;
  font-family: 'JetBrains Mono', 'Fira Code', monospace;
}

.traffic-value.upload {
  color: var(--accent-warning);
}

.traffic-value.download {
  color: var(--accent-secondary);
}

.traffic-value.total {
  color: var(--accent-purple);
}

.traffic-chart {
  background: var(--bg-card);
  border-radius: 8px;
  padding: 16px;
  border: 1px solid var(--border-tertiary);
}

.chart-bars {
  display: flex;
  align-items: end;
  gap: 2px;
  height: 60px;
}

.chart-bar {
  flex: 1;
  background: linear-gradient(to top, var(--accent-primary), var(--accent-secondary));
  border-radius: 2px;
  min-height: 4px;
  transition: var(--transition);
}

.chart-bar:hover {
  background: linear-gradient(to top, #1d4ed8, #0891b2);
}

.security-info {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.security-item {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px;
  border-radius: 8px;
  border: 1px solid var(--border-tertiary);
}

.security-item.safe {
  background: rgba(34, 197, 94, 0.1);
  border-color: rgba(34, 197, 94, 0.2);
}

.security-icon {
  font-size: 16px;
  width: 24px;
  text-align: center;
}

.security-text {
  flex: 1;
}

.security-title {
  font-size: 13px;
  font-weight: 600;
  color: var(--text-secondary);
  line-height: 1;
}

.security-desc {
  font-size: 11px;
  color: var(--text-muted);
  margin-top: 2px;
}

/* 自定义Naive UI组件样式 */
:deep(.n-input .n-input__input-el) {
  background: var(--bg-card);
  border-color: var(--border-tertiary);
  color: var(--text-secondary);
}

:deep(.n-input:hover .n-input__input-el) {
  border-color: var(--border-hover);
}

:deep(.n-input.n-input--focus .n-input__input-el) {
  border-color: var(--accent-primary);
}

:deep(.n-button) {
  border-color: var(--border-tertiary);
  color: var(--text-quaternary);
}

:deep(.n-button:hover) {
  border-color: var(--border-hover);
  color: var(--text-secondary);
}

/* 响应式调整 */
@media (max-width: 1200px) {
  .search-area {
    width: 250px;
  }

  .detail-grid {
    grid-template-columns: 1fr;
  }
}

/* 拖拽时的视觉反馈 */
.resize-handle-vertical:active,
.resize-handle-horizontal:active {
  background-color: var(--accent-primary) !important;
}
</style>
