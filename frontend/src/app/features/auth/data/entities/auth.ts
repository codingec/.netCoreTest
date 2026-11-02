import { DataTokenEntity } from "./data-token.entity"

export interface AuthEntity {
  code: string
  data: DataTokenEntity
}
