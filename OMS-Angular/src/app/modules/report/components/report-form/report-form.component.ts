import { Component, OnInit, Output, EventEmitter } from '@angular/core';
import * as moment from 'moment';
import { Observable } from 'rxjs';
import { FormControl } from '@angular/forms';
import { MatDatepicker, DateAdapter, MAT_DATE_FORMATS } from '@angular/material';
import { map, startWith } from 'rxjs/operators';
import { GraphService } from '@services/notification/graph.service';
import { ReportOptions, DateType } from '@shared/models/report-options.model';
import { DateFormatService } from '@services/report/date-format.service';
import { ReportDateAdapter } from './adapters/report-date.adapter';
import { formatStartDate } from './helpers/formatStartDateQuery';
import { formatEndDate } from './helpers/formatEndDateQuery';

@Component({
  selector: 'app-report-form',
  templateUrl: './report-form.component.html',
  styleUrls: ['./report-form.component.css'],
  providers: [
    ReportDateAdapter,
    {provide: DateAdapter, useClass: ReportDateAdapter},
  ],
})
export class ReportFormComponent implements OnInit {

  reportTypes: any[] = [
    { value: '0', name: 'Total' },
    { value: '1', name: 'SAIFI' },
    { value: '2', name: 'SAIDI' },
  ];

  datePickerTypes: any[] = [
    { value: DateType.Yearly, name: 'Yearly' },
    { value: DateType.Monthly, name: 'Monthly' },
    { value: DateType.Daily, name: 'Daily' },
  ];

  scopes: any[] = [
    { value: '0', name: 'All network elements', gid: 'No GID' }
  ];

  public selectedReportType;
  public selectedDateType;
  public filteredScopes: Observable<any[]>;
  public selectedScopeControl = new FormControl();
  public startDate = new FormControl();
  public endDate = new FormControl();
  public datePickerDisabled;
  
  private defaultDateType = DateType.Daily;

  @Output() generate = new EventEmitter<ReportOptions>();

  constructor(private graphService: GraphService, private dateFormatService: DateFormatService ) { }

  ngOnInit() {
    
    this.dateFormatService.setFormat(this.defaultDateType);

    this.graphService.getTopology().subscribe((graph) => {
      graph.Nodes.filter(node => node.DMSType == "ENERGYCONSUMER").forEach(node => this.scopes.push({
        value: node.Mrid,
        name: node.Name,
        gid: node.Id
      }))

      this.filteredScopes = this.selectedScopeControl.valueChanges
        .pipe(
          startWith(''),
          map(value => typeof value === 'string' ? value : value.name),
          map(name => name ? this.filterScopes(name) : this.scopes.slice())
        );
    });
  }

  filterScopes(name): any[] {
    const filterValue = name.toLowerCase();
    return this.scopes.filter(option => option.name.toLowerCase().indexOf(filterValue) === 0);
  }

  onDateTypeChangedHandler(dateType): void {
    this.dateFormatService.setFormat(dateType.value);
  }

  chosenYearHandler(event: moment.Moment, datePicker: MatDatepicker<moment.Moment>): void {
    this.startDate = new FormControl(moment());
    const ctrlValue = this.startDate.value;
    ctrlValue.year(event.year());
    this.startDate.setValue(ctrlValue);

    if(this.selectedDateType === DateType.Yearly) datePicker.close();
  }

  chosenMonthHandler(event: moment.Moment, datePicker: MatDatepicker<string>): void {
    const ctrlValue = this.startDate.value;
    ctrlValue.month(event.month());
    this.startDate.setValue(ctrlValue);

    if(this.selectedDateType === DateType.Monthly) datePicker.close();
  }

  chosenEndYearHandler(event: moment.Moment, datePicker: MatDatepicker<moment.Moment>): void {
    this.endDate = new FormControl(moment());
    const ctrlValue = this.endDate.value;
    ctrlValue.year(event.year());
    
    // @TODO: Da li potrebno dodati zastitu od datuma preko sadasnjeg ? 
    this.endDate.setValue(ctrlValue);


    if(this.selectedDateType === DateType.Yearly) datePicker.close();
  }

  chosenEndMonthHandler(event: moment.Moment, datePicker: MatDatepicker<string>): void {
    const ctrlValue = this.endDate.value;
    ctrlValue.month(event.month());
    this.endDate.setValue(ctrlValue);

    if(this.selectedDateType === DateType.Monthly) datePicker.close();
  }

  onSubmitHandler(): void {
    const options: ReportOptions = {
      Type: this.selectedReportType,
      ElementId: +this.selectedScopeControl.value,
      StartDate: formatStartDate(this.startDate.value, this.selectedDateType),
      EndDate: formatEndDate(this.endDate.value, this.selectedDateType)
    }

    this.generate.emit(options);

    console.log(options);
  }


}
