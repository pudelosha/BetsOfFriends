import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule } from '@ionic/angular';
import { TranslateModule } from '@ngx-translate/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { TitleService } from 'src/app/services/title.service';

@Component({
  selector: 'app-faq',
  templateUrl: './faq.page.html',
  styleUrls: ['./faq.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule, FormsModule, TranslateModule],
})
export class FaqPage implements OnInit {
  expandedQuestionId: string | null = null;
  isLoading = false;

  constructor(private titleService: TitleService) {}

  ngOnInit() {
    this.titleService.setTitle('FAQ.TITLE');
  }

  ionViewWillEnter() {
    this.titleService.setTitle('FAQ.TITLE');
  }

  toggleFaq(faq: any) {
    this.expandedQuestionId = this.expandedQuestionId === faq.id ? null : faq.id;
  }

  faqs = [
    // GENERAL
    { id: '1', question: 'Is this a gambling site?', answer: 'No, the platform is for fun only – no real money is involved.', category: 'General' },
    { id: '2', question: 'Are there any fees?', answer: 'No, all features are completely free.', category: 'General' },
    { id: '3', question: 'Can I use the platform on mobile?', answer: 'Yes, it’s optimized for both desktop and mobile browsers.', category: 'General' },
    { id: '4', question: 'Can I change the language?', answer: 'Yes, there’s a floating button to switch languages.', category: 'General' },

    // ACCOUNT
    { id: '5', question: 'Can I delete my account?', answer: 'Yes, you can request account deletion via support.', category: 'Account' },
    { id: '6', question: 'How do I change my email?', answer: 'Go to profile settings and select “Change Email”.', category: 'Account' },
    { id: '7', question: 'Is my data secure?', answer: 'Yes, we follow privacy regulations and secure storage.', category: 'Account' },

    // PROFILE
    { id: '8', question: 'Can I use a nickname?', answer: 'Yes, you can update your nickname in your profile settings.', category: 'Profile' },
    { id: '9', question: 'What is dark mode?', answer: 'It’s a visual preference you can enable in profile settings.', category: 'Profile' },

    // TOURNAMENTS
    { id: '10', question: 'How do I create a tournament?', answer: 'Use the Tournament Creator form in the app.', category: 'Tournaments' },
    { id: '11', question: 'How do I leave a tournament?', answer: 'Use the “Quit” button in the tournament dashboard.', category: 'Tournaments' },
    { id: '12', question: 'How many tournaments can I join?', answer: 'As many as you are invited to.', category: 'Tournaments' },
    { id: '13', question: 'Can tournaments be public?', answer: 'Not yet. All tournaments are private and invitation-based.', category: 'Tournaments' },

    // PREDICTIONS
    { id: '14', question: 'How do predictions work?', answer: 'You choose the outcome of each match before it starts.', category: 'Predictions' },
    { id: '15', question: 'Can I update my predictions?', answer: 'Yes, until the match starts.', category: 'Predictions' },
    { id: '16', question: 'Are my predictions visible to others?', answer: 'Yes, but only after the match has started.', category: 'Predictions' },
    { id: '17', question: 'Can I see other participants’ predictions?', answer: 'Yes, if the match has started.', category: 'Predictions' },

    // SCORING
    { id: '18', question: 'How are points awarded?', answer: 'Points depend on prediction accuracy: win, draw, or score.', category: 'Scoring' },
    { id: '19', question: 'What happens after a match ends?', answer: 'Scores are finalized, and the leaderboard updates.', category: 'Scoring' },

    // INVITES
    { id: '20', question: 'Can I invite friends?', answer: 'Yes, each tournament has an invite option for organizers.', category: 'Invites' },
    { id: '21', question: 'Can I remove someone from my tournament?', answer: 'Yes, if you are the organizer.', category: 'Invites' },
    { id: '22', question: 'Can I resend invitations?', answer: 'Yes, use the “Resend Invite” button in the dashboard.', category: 'Invites' },

    // ROLES
    { id: '23', question: 'What’s the difference between admin and organizer?', answer: 'Organizers can invite/remove users; admins can manage match data.', category: 'Roles' },
    { id: '24', question: 'Can I become an admin?', answer: 'Only the organizer can assign admin privileges.', category: 'Roles' },

    // IMPORTS
    { id: '25', question: 'Where can I get the Excel template?', answer: 'Download it from the Downloads page.', category: 'Imports' },
    { id: '26', question: 'Can I import matches from Excel?', answer: 'Yes, custom tournaments support Excel import.', category: 'Imports' },
    { id: '27', question: 'Can I import players from another tournament?', answer: 'Not yet – feature planned.', category: 'Imports' },

    // BILLING (even though everything is free)
    { id: '28', question: 'Will this ever cost money?', answer: 'We may add premium features later, but core will remain free.', category: 'Billing' },
    { id: '29', question: 'Do I need a subscription?', answer: 'No subscription is required to use any features.', category: 'Billing' },

    // SUPPORT
    { id: '30', question: 'How can I contact support?', answer: 'Use the support form on the Info & Support page.', category: 'Support' }
  ].sort((a, b) => a.category.localeCompare(b.category));
}
