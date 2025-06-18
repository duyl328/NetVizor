/**
 * Time:2025/4/25 20:13 16
 * Name:imageInfoCacheManager.ts
 * Path:src/utils
 * ProjectName:argus-src
 * Author:charlatans
 *
 *  Il n'ya qu'un héroïsme au monde :
 *     c'est de voir le monde tel qu'il est et de l'aimer.
 */

import { getImageInfo } from '@/services/imageService'
import type { ImageInfo } from '@/types/image.type'

/**
 * 负责处理与图像信息相关的缓存机制
 */
class ImageInfoCacheManager {
  private static instance: ImageInfoCacheManager
  private maxSize: number
  private cache: Map<string, ImageInfo>

  private constructor(maxSize = 50) {
    this.cache = new Map()
    this.maxSize = maxSize
  }

  static getInstance(): ImageInfoCacheManager {
    if (!ImageInfoCacheManager.instance) {
      ImageInfoCacheManager.instance = new ImageInfoCacheManager()
    }
    return ImageInfoCacheManager.instance
  }

  async preloadImageInfo(src: string): Promise<void> {
    if (this.cache.has(src)) return

    return new Promise((resolve, reject) => {
      getImageInfo(src).then((res) => {
        let info = JSON.parse(res)
        this.addToCache(src, info)
        resolve()
      })
    })
  }

  getImageInfo(src: string): Promise<ImageInfo> {
    return new Promise((resolve, reject) => {
      let newVar = this.cache.get(src)
      if (newVar !== undefined && newVar !== null) {
        resolve(newVar)
      } else {
        getImageInfo(src).then((res) => {
          let info = JSON.parse(res)
          this.addToCache(src, info)
          resolve(info)
        }).catch(reject)
      }
    })
  }

  getImageInfoSync(src: string): ImageInfo | undefined {
    return this.cache.get(src)
  }

  private addToCache(src: string, info: ImageInfo) {
    if (this.cache.size >= this.maxSize) {
      const firstKey = this.cache.keys().next().value
      this.cache.delete(firstKey)
    }
    this.cache.set(src, info)
  }

  clearCache() {
    this.cache.clear()
  }
}

export default ImageInfoCacheManager
