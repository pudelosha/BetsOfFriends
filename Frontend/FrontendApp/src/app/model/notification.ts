export interface NotificationDto {
  notificationId: number;
  title: string;
  message: string;
  route?: string;
  type?: string;
  senderUserId?: string;
  senderDisplayName?: string;
  tournamentId?: number;
  createdAt: string;
  isRead: boolean;
}

  
