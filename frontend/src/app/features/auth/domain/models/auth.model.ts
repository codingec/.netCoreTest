import { DataTokenModel } from "./data-token.model"

export interface AuthModel {
  code: string
  data: DataTokenModel
}
