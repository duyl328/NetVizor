/**
 * Time:2025/4/24 18:13 49
 * Name:imageCacheManager.ts
 * Path:src/utils
 * ProjectName:argus-src
 * Author:charlatans
 *
 *  Il n'ya qu'un héroïsme au monde :
 *     c'est de voir le monde tel qu'il est et de l'aimer.
 */

/**
 * 照片缓存管理器
 */
class ImageCacheManager {
  private static instance: ImageCacheManager
  private cache: Map<string, HTMLImageElement>
  private maxSize: number

  private constructor(maxSize = 20) {
    this.cache = new Map()
    this.maxSize = maxSize
  }

  static getInstance(): ImageCacheManager {
    if (!ImageCacheManager.instance) {
      ImageCacheManager.instance = new ImageCacheManager()
    }
    return ImageCacheManager.instance
  }

  async preloadImage(src: string): Promise<void> {
    if (this.cache.has(src)) return

    return new Promise((resolve, reject) => {
      const img = new Image()
      img.src = src
      img.onload = () => {
        this.addToCache(src, img)
        resolve()
      }
      img.onerror = reject
    })
  }

  private addToCache(src: string, img: HTMLImageElement) {
    if (this.cache.size >= this.maxSize) {
      const firstKey = this.cache.keys().next().value
      this.cache.delete(firstKey)
    }
    this.cache.set(src, img)
  }

  getFromCache(src: string): HTMLImageElement | undefined {
    return this.cache.get(src)
  }

  clearCache() {
    this.cache.clear()
  }
}

export default ImageCacheManager
