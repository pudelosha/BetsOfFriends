import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CreatePredefinedTournamentPage } from './create-predefined-tournament.page';

describe('CreatePredefinedTournamentPage', () => {
  let component: CreatePredefinedTournamentPage;
  let fixture: ComponentFixture<CreatePredefinedTournamentPage>;

  beforeEach(() => {
    fixture = TestBed.createComponent(CreatePredefinedTournamentPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
