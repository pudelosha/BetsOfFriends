import { Injectable } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

@Injectable({
  providedIn: 'root',
})
export class BackendMessageService {
  private readonly messageKeyByText: Record<string, string> = {
    'login successful': 'AUTH_STATUS.LOGIN_SUCCESS',
    'login failed': 'AUTH_STATUS.LOGIN_FAILED',
    'invalid credentials': 'AUTH_STATUS.INVALID_CREDENTIALS',
    'invalid email or password please try again': 'AUTH_STATUS.INVALID_CREDENTIALS',
    'account is locked due to multiple failed login attempts': 'AUTH_STATUS.ACCOUNT_LOCKED',
    'your account is locked contact support': 'AUTH_STATUS.ACCOUNT_LOCKED',
    'email is not confirmed please check your inbox': 'AUTH_STATUS.EMAIL_NOT_CONFIRMED',
    'user not found': 'AUTH_STATUS.USER_NOT_FOUND',
    'user not found please register first': 'AUTH_STATUS.USER_NOT_FOUND_REGISTER',

    'registration successful please check your email': 'AUTH_STATUS.REGISTRATION_SUCCESS',
    'registration successful please check your email to confirm your account': 'AUTH_STATUS.REGISTRATION_SUCCESS',
    'registration successful but the confirmation email could not be sent please use resend activation email from the login page': 'AUTH_STATUS.REGISTRATION_EMAIL_WARNING',
    'registration failed due to validation errors': 'AUTH_STATUS.REGISTRATION_FAILED',
    'email is already in use': 'AUTH_STATUS.EMAIL_ALREADY_IN_USE',
    'registration is temporarily unavailable please try again later': 'AUTH_STATUS.TEMPORARILY_UNAVAILABLE',

    'email confirmed': 'AUTH_STATUS.EMAIL_CONFIRMED',
    'email confirmed successfully': 'AUTH_STATUS.EMAIL_CONFIRMED',
    'unable to confirm email try again later': 'AUTH_STATUS.CONFIRMATION_FAILED',
    'invalid or expired confirmation token': 'AUTH_STATUS.INVALID_CONFIRMATION_LINK',
    'confirmation email sent successfully': 'AUTH_STATUS.CONFIRMATION_EMAIL_SENT',
    'activation email resent successfully': 'AUTH_STATUS.CONFIRMATION_EMAIL_SENT',
    'could not send confirmation email please try again later': 'AUTH_STATUS.CONFIRMATION_EMAIL_SEND_FAILED',
    'this account is already confirmed': 'AUTH_STATUS.EMAIL_ALREADY_CONFIRMED',
    'email already confirmed': 'AUTH_STATUS.EMAIL_ALREADY_CONFIRMED',

    'account setup successful you can now log in': 'AUTH_STATUS.ACCOUNT_SETUP_SUCCESS',
    'account setup completed successfully': 'AUTH_STATUS.ACCOUNT_SETUP_SUCCESS',
    'failed to set password': 'AUTH_STATUS.SET_PASSWORD_FAILED',
    'account setup is temporarily unavailable please try again later': 'AUTH_STATUS.TEMPORARILY_UNAVAILABLE',

    'if your email exists in our system a reset link has been sent': 'AUTH_STATUS.PASSWORD_RESET_SENT',
    'password updated successfully': 'AUTH_STATUS.PASSWORD_UPDATED',
    'invalid email address or user not found': 'AUTH_STATUS.INVALID_EMAIL_OR_USER_NOT_FOUND',

    'profile updated successfully': 'PROFILE.PROFILE_UPDATED',
    'failed to update profile': 'PROFILE.PROFILE_UPDATE_FAILED',
    'email updated successfully': 'PROFILE.EMAIL_UPDATED',
    'failed to update email check your password': 'PROFILE.EMAIL_UPDATE_FAILED',
    'failed to update password check your current password': 'PROFILE.PASSWORD_UPDATE_FAILED',
    'account deleted successfully': 'PROFILE.ACCOUNT_DELETED',
    'failed to delete account check your password': 'PROFILE.ACCOUNT_DELETE_FAILED',

    'user suspended successfully': 'USER_MANAGER.SUSPENDED',
    'failed to suspend user': 'USER_MANAGER.SUSPEND_FAILED',
    'user unsuspended successfully': 'USER_MANAGER.UNSUSPENDED',
    'failed to unsuspend user': 'USER_MANAGER.UNSUSPEND_FAILED',
    'user deleted successfully': 'USER_MANAGER.DELETED',
    'failed to delete user': 'USER_MANAGER.DELETE_FAILED',
    'you cannot delete a super admin': 'USER_MANAGER.CANNOT_DELETE_SUPER_ADMIN',
    'unauthorized access': 'AUTH_GUARD.PERMISSION_DENIED',

    'tournament not found': 'MANAGE_PARTICIPANTS.TOURNAMENT_NOT_FOUND',
    'user authentication failed': 'AUTH_GUARD.PERMISSION_DENIED',
    'you are not authorized to exclude participants': 'USERS_LIST.EXCLUDE_UNAUTHORIZED',
    'you cannot exclude yourself': 'USERS_LIST.CANNOT_EXCLUDE_SELF',
    'you cannot exclude the tournament creator': 'USERS_LIST.CANNOT_EXCLUDE_CREATOR',
    'user is not a participant': 'USERS_LIST.USER_NOT_PARTICIPANT',
    'an error occurred while excluding the participant': 'USERS_LIST.EXCLUDE_FAILED',
    'you are not authorized to accept participants': 'PENDING_REQUESTS.ACCEPT_UNAUTHORIZED',
    'no join request found for this user': 'PENDING_REQUESTS.NO_JOIN_REQUEST',
    'an error occurred while accepting the participant': 'PENDING_REQUESTS.ACCEPT_FAILED',
    'you are not authorized to resend invites': 'PENDING_INVITES.RESEND_UNAUTHORIZED',
    'no invitation found for this user': 'PENDING_INVITES.NO_INVITATION',
    'an error occurred while resending the invitation': 'PENDING_INVITES.RESEND_FAILED',
    'nickname cannot be empty or exceed 20 characters': 'INVITE.NICKNAME_INVALID',
    'this nickname is already taken please choose a different one': 'INVITE.NICKNAME_TAKEN',
    'this nickname is already taken in the tournament please choose a different one': 'JOIN_MODAL.NICKNAME_TAKEN',
    'no accepted assignment found cannot update nickname': 'INVITE.NO_ACCEPTED_ASSIGNMENT',
    'nickname updated successfully': 'INVITE.NICKNAME_UPDATED',
    'an unexpected error occurred while updating your nickname please try again': 'INVITE.UPDATE_FAILED',
    'tournament invitation could not be accepted you may not be invited': 'INVITE.ACCEPT_FAILED',
    'an unexpected error occurred while accepting the invitation please try again': 'INVITE.ACCEPT_FAILED',
    'you have already requested or joined this tournament': 'JOIN_MODAL.ALREADY_REQUESTED',
    'an error occurred while requesting to join the tournament please try again': 'JOIN_MODAL.REQUEST_FAILED',
    'an unexpected error occurred while requesting to join the tournament': 'JOIN_MODAL.REQUEST_FAILED',

    'invalid request data': 'AUTH_STATUS.INVALID_REQUEST',
    'email is required': 'AUTH_STATUS.EMAIL_REQUIRED',
    'password must be at least 8 characters long': 'AUTH_STATUS.PASSWORD_MIN_LENGTH',
    'you must accept terms and conditions': 'AUTH_STATUS.TERMS_REQUIRED',
    'invalid user id': 'AUTH_STATUS.INVALID_USER_ID',
    'server error please try again later': 'AUTH_STATUS.SERVER_ERROR',
    'an unexpected error occurred please try again later': 'AUTH_STATUS.UNEXPECTED_ERROR',
    'an unexpected error occurred please try again': 'AUTH_STATUS.UNEXPECTED_ERROR',
    'an unexpected error occurred': 'AUTH_STATUS.UNEXPECTED_ERROR',
    'an error occurred please try again': 'AUTH_STATUS.UNEXPECTED_ERROR',
    'something went wrong please try again': 'AUTH_STATUS.UNEXPECTED_ERROR',
    'unable to reach the server please check your connection and try again': 'AUTH_STATUS.NETWORK_ERROR',
  };

  constructor(private translate: TranslateService) {}

  translateMessage(message?: string | null, fallbackKey?: string, preferFallbackForUnknown = false): string {
    const key = this.findTranslationKey(message);
    if (key) {
      return this.translate.instant(key);
    }

    if ((!message || preferFallbackForUnknown) && fallbackKey) {
      return this.translate.instant(fallbackKey);
    }

    return message || (fallbackKey ? this.translate.instant(fallbackKey) : '');
  }

  private findTranslationKey(message?: string | null): string | undefined {
    if (!message) return undefined;
    return this.messageKeyByText[this.normalize(message)];
  }

  private normalize(message: string): string {
    return message
      .trim()
      .toLowerCase()
      .replace(/[.!?,]/g, '')
      .replace(/\s+/g, ' ');
  }
}
