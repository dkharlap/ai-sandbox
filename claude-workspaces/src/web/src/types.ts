export type AuthConfig = {
  devMode: boolean
}

export type Session = {
  userId: string
  email: string | null
  name: string | null
}

export type User = {
  id: string
  email: string
  firstName: string | null
  lastName: string | null
}

export type Model = {
  id: string
  displayName: string
  vendor: string
}

export type Preference = {
  modelId: string
  displayName: string
  vendor: string
  rank: number
}
