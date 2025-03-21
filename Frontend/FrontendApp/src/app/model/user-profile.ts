export interface UserProfile {
  id: string;
  email: string;
  nickname?: string;
  location?: string;
  memberSince: string;
  language: string;
  darkMode: boolean;
}