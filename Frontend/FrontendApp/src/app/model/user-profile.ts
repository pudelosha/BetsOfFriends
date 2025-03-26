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
  userId: string; // Unique user identifier (can be string or number depending on your backend)
  userName: string;
  userEmail: string;
  userStatus: string;

  status: 'New' | 'Invited' | 'Accepted' | 'Banned';
  userRole: 'Player' | 'Admin';

  tournamentAdminCount: number;
  tournamentParticipantCount: number;

  memberSince: string; // ISO date string, e.g. "2023-06-14T12:00:00Z"
}
