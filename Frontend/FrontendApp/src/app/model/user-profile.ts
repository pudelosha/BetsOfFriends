export interface UserProfile {
  id: string;
  email: string;
  nickname?: string;
  location?: {
    countryId: number;
    name: string;
  } | null;
  memberSince: string;
  language: string;
  darkMode: boolean;
}

export interface ApplicationUser {
  userId: string;
  userName: string;
  userEmail: string;
  userStatus: string;

  status: 'New' | 'Invited' | 'Accepted' | 'Banned';
  userRole: 'Player' | 'Admin';

  tournamentAdminCount: number;
  tournamentParticipantCount: number;

  memberSince: string;
}
