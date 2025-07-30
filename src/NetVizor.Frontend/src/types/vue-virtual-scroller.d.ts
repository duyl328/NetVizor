declare module 'vue-virtual-scroller' {
  import { DefineComponent } from 'vue'

  export const RecycleScroller: DefineComponent<{
    items: any[]
    itemSize: number | string
    keyField?: string
    direction?: 'vertical' | 'horizontal'
    listTag?: string
    itemTag?: string
    listClass?: string | object | any[]
    itemClass?: string | object | any[]
    gridItems?: number
    sizeField?: string
    typeField?: string
    buffer?: number
    pageMode?: boolean
    prerender?: number
    emitUpdate?: boolean
  }>

  export const DynamicScroller: DefineComponent<{
    items: any[]
    minItemSize: number | string
    keyField?: string
    direction?: 'vertical' | 'horizontal'
    listTag?: string
    itemTag?: string
    listClass?: string | object | any[]
    itemClass?: string | object | any[]
    buffer?: number
    pageMode?: boolean
    prerender?: number
    emitUpdate?: boolean
  }>

  export const DynamicScrollerItem: DefineComponent<{
    item: any
    index: number
    active: boolean
    sizeDependencies?: any[]
    watchData?: boolean
    tag?: string
    emitResize?: boolean
    onResize?: () => void
  }>

  const plugin: {
    install: (app: any, options?: any) => void
  }

  export default plugin
}