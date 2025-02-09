import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ResendActivationPage } from './resend-activation.page';

describe('ResendActivationPage', () => {
  let component: ResendActivationPage;
  let fixture: ComponentFixture<ResendActivationPage>;

  beforeEach(() => {
    fixture = TestBed.createComponent(ResendActivationPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
