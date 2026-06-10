import { useContext } from 'react'
import AuthContext from './authContext'

function useSession() {
  return useContext(AuthContext)
}

export default useSession
