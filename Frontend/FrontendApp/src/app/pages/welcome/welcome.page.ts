import { Component  } from '@angular/core';
import { IonicModule } from '@ionic/angular';
import { ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { LanguageService } from 'src/app/services/language.service';
import { TranslateModule } from '@ngx-translate/core';
import { LanguageFabComponent } from '../language/language-fab/language-fab.component';


@Component({
  selector: 'app-welcome',
  templateUrl: './welcome.page.html',
  styleUrls: ['./welcome.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule, TranslateModule, LanguageFabComponent],
})
export class WelcomePage {
  parallaxOffset = 0;

  constructor(private router: Router, private languageService: LanguageService) {
    this.languageService.initLanguage();
  }

  onScroll(event: any) {
    const scrollTop = event.detail.scrollTop;
    this.parallaxOffset = scrollTop * 0.4; // Adjust speed
  }

  navigateToLogin() {
    this.router.navigate(['/login']);
  }
  
  navigateToRegister() {
    this.router.navigate(['/register']);
  }  
}
