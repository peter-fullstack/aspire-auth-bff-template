import { createContext } from 'react'

export type User = {
  email: string
  permissions: string[]
  roles: string[]
}

export type AuthContextData = {
  user?: User
  isAuthenticated: boolean
  loadingUserData: boolean
  authError: string | null
  checkAuthStatus: () => Promise<void>
  login: () => void
  signOut: () => void
}

const AuthContext = createContext({} as AuthContextData)

export default AuthContext
