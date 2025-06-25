export interface SupportMessage {
  email: string;
  subject: string;
  message: string;
  language: string;
}

export interface TournamentMessage {
  id: number;
  tournamentId: number;
  userId: string;
  userName: string;
  content: string;
  createdAt: string; // ISO date string
}

export interface PrivateMessage {
  id: number;
  fromUserId: string;
  toUserId: string;
  fromUserName: string;
  content: string;
  sentAt: string; // ISO date string
  isRead: boolean;
}

export interface CreateMessageResult {
  success: boolean;
  errorMessage?: string;
}

